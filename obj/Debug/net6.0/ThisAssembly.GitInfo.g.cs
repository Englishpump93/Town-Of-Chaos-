// <auto-generated />
#define GLOBALNAMESPACE
#define NOMETADATA
#pragma warning disable 0436

#if ADDMETADATA
[assembly: System.Reflection.AssemblyMetadata("GitInfo.IsDirty", ThisAssembly.Git.IsDirtyString)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.Branch", ThisAssembly.Git.Branch)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.Commit", ThisAssembly.Git.Commit)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.Sha", ThisAssembly.Git.Sha)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.BaseVersion.Major", ThisAssembly.Git.BaseVersion.Major)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.BaseVersion.Minor", ThisAssembly.Git.BaseVersion.Minor)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.BaseVersion.Patch", ThisAssembly.Git.BaseVersion.Patch)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.Commits", ThisAssembly.Git.Commits)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.Tag", ThisAssembly.Git.Tag)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.BaseTag", ThisAssembly.Git.BaseTag)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.SemVer.Major", ThisAssembly.Git.SemVer.Major)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.SemVer.Minor", ThisAssembly.Git.SemVer.Minor)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.SemVer.Patch", ThisAssembly.Git.SemVer.Patch)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.SemVer.Label", ThisAssembly.Git.SemVer.Label)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.SemVer.DashLabel", ThisAssembly.Git.SemVer.DashLabel)]
[assembly: System.Reflection.AssemblyMetadata("GitInfo.SemVer.Source", ThisAssembly.Git.SemVer.Source)]
#endif

#if LOCALNAMESPACE
namespace 
{
#endif
  /// <summary>Provides access to the current assembly information.</summary>
  partial class ThisAssembly
  {
    /// <summary>Provides access to the git information for the current assembly.</summary>
    public partial class Git
    {
      /// <summary>IsDirty: true</summary>
      public const bool IsDirty = true;

      /// <summary>IsDirtyString: true</summary>
      public const string IsDirtyString = "true";

      /// <summary>Branch: main</summary>
      public const string Branch = "main";

      /// <summary>Commit: b54eca6</summary>
      public const string Commit = "b54eca6";

      /// <summary>Sha: b54eca6146f4bf6bc42e18d60e10c23cd7669626</summary>
      public const string Sha = "b54eca6146f4bf6bc42e18d60e10c23cd7669626";

      /// <summary>Commits on top of base version: 24</summary>
      public const string Commits = "24";

      /// <summary>Tag: Chaos2.8.1-9-gb54eca6</summary>
      public const string Tag = "Chaos2.8.1-9-gb54eca6";

      /// <summary>Base tag: Chaos2.8.1</summary>
      public const string BaseTag = "Chaos2.8.1";

      /// <summary>Provides access to the base version information used to determine the <see cref="SemVer" />.</summary>      
      public partial class BaseVersion
      {
        /// <summary>Major: 0</summary>
        public const string Major = "0";

        /// <summary>Minor: 0</summary>
        public const string Minor = "0";

        /// <summary>Patch: 0</summary>
        public const string Patch = "0";
      }

      /// <summary>Provides access to SemVer information for the current assembly.</summary>
      public partial class SemVer
      {
        /// <summary>Major: 0</summary>
        public const string Major = "0";

        /// <summary>Minor: 0</summary>
        public const string Minor = "0";

        /// <summary>Patch: 24</summary>
        public const string Patch = "24";

        /// <summary>Label: </summary>
        public const string Label = "";

        /// <summary>Label with dash prefix: </summary>
        public const string DashLabel = "";

        /// <summary>Source: Default</summary>
        public const string Source = "Default";
      }
    }
  }
#if LOCALNAMESPACE
}
#endif
#pragma warning restore 0436
