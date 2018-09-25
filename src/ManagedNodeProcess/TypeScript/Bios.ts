/// <reference types="node" />

const eol = require('os').EOL;
const i = process.stdin;
let s = -1;
let e = -1;
let ch = '';

const onChunk = c => {
    const m = /<\[JS\((\d*)\)\[((?:.|\s)*)]]>/.exec(c);

    if (m) {
        try {
            const r = (1, eval)(m[2]);

            if (r instanceof Promise) {
                r['then'](r => process.stdout.write('<[JS(' + m[1] + ')[' + JSON.stringify(r) + ']]>' + eol))
                 ['catch'](er => process.stderr.write('<[JS(' + m[1] + ')[' + (typeof er === 'string' ? er : er.message) + ']]>' + eol));
            }
            else {
                process.stdout.write('<[JS(' + m[1] + ')[' + JSON.stringify(r) + ']]>' + eol);
            }
        }
        catch (er) {
            process.stderr.write('<[JS(' + m[1] + ')[' + (typeof er === 'string' ? er : er.message) + ']]>' + eol);
        }
    }
};

const pump = (b: string | Buffer) => {
    ch += b.toString();
    s = ch.indexOf('<[');
    e = ch.indexOf(']>');

    while (s >= 0 && e >= 0) {
        onChunk(ch.substring(s, e + 2));
        ch = ch.substring(e + 2);
        s = ch.indexOf('<[');
        e = ch.indexOf(']>');
    }
};
i.on('data', pump);
