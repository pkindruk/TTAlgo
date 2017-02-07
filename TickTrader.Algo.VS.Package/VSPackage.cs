﻿//------------------------------------------------------------------------------
// <copyright file="VSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace TickTrader.Algo.VS.Package
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.06", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideProjectFactory(typeof(CsProjectFactory), "TT Algo Project", null, null, null, @"..\Templates\Projects")]
    [Guid(VSPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class VSPackage : Microsoft.VisualStudio.Shell.Package
    {
        private IVsOutputWindow outputWnd;
        private IVsOutputWindowPane generalOuputPane;
        private IVsOutputWindowPane buildOutputPane;

        /// <summary>
        /// VSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d6b700f3-8d25-45e2-8069-163e017c27d8";

        /// <summary>
        /// Initializes a new instance of the <see cref="VSPackage"/> class.
        /// </summary>
        public VSPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            Trace.WriteLine(string.Format("Entering constructor for: {0}", this));
        }

        public IVsOutputWindow OutputWnd
        {
            get
            {
                if (outputWnd == null)
                    outputWnd = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                return outputWnd;
            }
        }

        public IVsOutputWindowPane OutputPane_General
        {
            get
            {
                if (generalOuputPane == null)
                {
                    Guid paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
                    var outputWindow = OutputWnd;
                    int hr = outputWindow.CreatePane(paneGuid, "General", 1, 0);
                    hr = outputWindow.GetPane(paneGuid, out generalOuputPane);
                }
                return generalOuputPane;
            }
        }

        public IVsOutputWindowPane OutputPane_Build
        {
            get
            {
                if (buildOutputPane == null)
                {
                    Guid paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid;
                    var outputWindow = OutputWnd;
                    int hr = outputWindow.CreatePane(paneGuid, "Build", 1, 0);
                    hr = outputWindow.GetPane(paneGuid, out buildOutputPane);
                }
                return buildOutputPane;
            }
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format("Entering Initialize() of: {0}", this));
            base.Initialize();
            this.RegisterProjectFactory(new CsProjectFactory(this));
        }

        #endregion
    }
}
