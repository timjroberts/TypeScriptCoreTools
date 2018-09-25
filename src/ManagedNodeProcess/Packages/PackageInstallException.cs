using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ManagedNodeProcess.Packages
{
    /// <summary>
    /// The exception that is thrown when a required package of a Node process could not be installed.
    /// </summary>
    [Serializable]
    public class PackageInstallException : Exception
    {
        /// <summary>
        /// Creates a default <see cref="PackageInstallException" />.
        /// </summary>
        public PackageInstallException()
        { }
        
        /// <summary>
        /// Creates a <see cref="PackageInstallException" /> that identifies the uninstallable packages.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="uninstallablePackages">The list of package specification that caused the exception.</param>
        public PackageInstallException(string message, IList<string> uninstallablePackages)
            : base(message)
        {
            UninstallablePackages = uninstallablePackages;
        }

        /// <summary>
        /// Creates a <see cref="PackageInstallException" /> that identifies the uninstallable packages.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="uninstallablePackages">The list of package specification that caused the exception.</param>
        /// <param name="inner">An inner exception that was a root cause for failing to install packages.</param>
        public PackageInstallException(string message, IList<string> uninstallablePackages, Exception inner)
            : base(message, inner)
        {
            UninstallablePackages = uninstallablePackages;
        }

        protected PackageInstallException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        /// <summary>
        /// Gets the list of package specifications that were unable to be installed.
        /// </summary>
        /// <returns>A list of package specfications in the form 'package-name@version'.</returns>
        public IList<string> UninstallablePackages { get; protected set; }
    }
}
