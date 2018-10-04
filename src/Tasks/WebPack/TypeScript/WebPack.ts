/// <reference types="node" />
/// <reference types="webpack" />

import * as path from "path";
import * as fs from "fs";
import * as os from "os";
import * as webpack from "webpack";

type BundlePackage = {
    packageName: string;
    resolvedDirectoryPath: string;
    isBundle: boolean;
}

const DebugConfigurationName: string = "Debug";
const ProjectDirectory: string = process.cwd();

function GetDevToolConfiguration(configuration: string): webpack.Configuration
{
    if (configuration === DebugConfigurationName)
    {
        return {
            devtool: "source-map",
            module:
            {
                rules: [
                    { enforce: "pre", test: /\.js$/, loader: require.resolve("source-map-loader") }
                ]
            }
        }
    }

    return {
        devtool: false
    }
}

function GetExternalPackagesFromManifestFile(manifestFilePath: string): webpack.ExternalsObjectElement
{
    var manifest = JSON.parse(fs.readFileSync(path.join(manifestFilePath, "index.manifest.json")).toString());
    var externalsObj = { };

    for (var packageName in manifest.content)
    {
        externalsObj[packageName] = `${manifest.name}('${manifest.content[packageName].id}')`;
    }

    return externalsObj;
}

function GetExternalPackages(manifestFilePath: string): webpack.ExternalsObjectElement
function GetExternalPackages(manifestFilePaths: string[]): webpack.ExternalsObjectElement
function GetExternalPackages(p0: string[] | string): webpack.ExternalsObjectElement
{
    return (Array.isArray(p0))
        ? p0.reduce((agg: webpack.ExternalsObjectElement, m) => ({ ...agg, ...GetExternalPackages(m) }), { })
        : GetExternalPackagesFromManifestFile(p0);
}

function CompileConfiguration(webPackConfig: webpack.Configuration): Promise<void>
{
    return new Promise<void>((resolve, reject) =>
    {
        const compiler = webpack(webPackConfig);

        compiler.run((err: Error, stats: webpack.Stats) =>
        {
            if (err) return reject(err);
        
            if (stats.hasErrors()) return reject(new Error(stats.toString()));

            resolve();
        });
    });
}

function RewriteManifest(manifestFilePath: string, contextPath: string, bundlePackages: BundlePackage[], esTarget: string): void
{
    var manifest = JSON.parse(fs.readFileSync(manifestFilePath).toString());
    var newManifest = { name: manifest.name, esTarget, content: { } };

    for (var contentPath in manifest.content)
    {
        var absoluteContentPath = path.normalize(path.join(contextPath, contentPath));
        var bundledPackage = bundlePackages.find(target => absoluteContentPath.startsWith(target.resolvedDirectoryPath));

        if (!bundledPackage) continue;

        newManifest.content[bundledPackage.packageName] = manifest.content[contentPath];
    }

    fs.writeFileSync(manifestFilePath, JSON.stringify(newManifest, null, 2));
}

async function PackProject(libraryName: string, bundleAsLibrary: boolean, configuration: string, bundlePackages: BundlePackage[]): Promise<void>
{
    const bundleTargets = bundlePackages.filter(p => !p.isBundle);
    const externalTargets = bundlePackages.filter(p => p.isBundle);
    const manifestFilePath = bundleAsLibrary ? path.join(ProjectDirectory, `bin/${configuration}/js/index.manifest.json`) : "";

    let webPackConfig: webpack.Configuration =
    {
        stats: { modules: false },
        mode: configuration === DebugConfigurationName ? "development" : "production",
        context: ProjectDirectory,
        entry: bundleTargets.map(target => target.packageName),
        output: {
            pathinfo: configuration === DebugConfigurationName,
            path: path.join(ProjectDirectory, `bin/${configuration}/js`),
            filename: "index.js",
            library: libraryName
        },
        resolve: {
            alias: bundleTargets.reduce((agg, target) => ({ ...agg,  [target.packageName]: target.resolvedDirectoryPath}), { })
        },
        optimization: {
            minimize: false // configuration !== DebugConfigurationName  [currently hangs webpack when true]
        },
        plugins: [
            new webpack.DefinePlugin(
                {
                    "process.env": {
                        "CONFIGURATION": JSON.stringify(configuration),
                        "ES_TARGET": JSON.stringify('ES5'),
                        "NODE_ENV": JSON.stringify(configuration === DebugConfigurationName ? "development" : "production")
                    }
                }
            ),
            ...bundleAsLibrary
                ?   [
                        new webpack.DllPlugin({
                                context: ProjectDirectory,
                                path: manifestFilePath,
                                name: libraryName
                            }
                        )
                    ]
                :   []
        ],
        externals: [
            GetExternalPackages(externalTargets.map(target => target.resolvedDirectoryPath))
        ],
        ...GetDevToolConfiguration(configuration)
    };

    try
    {
        await CompileConfiguration(webPackConfig);

        if (bundleAsLibrary)
        {
            RewriteManifest(manifestFilePath, ProjectDirectory, bundlePackages, "ES5");
        }
    }
    catch (compileErr)
    {
        throw new Error(`Error packing (${ProjectDirectory}):${os.EOL}${compileErr}`);
    }
}

global["PackProject"] = PackProject;