using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Fickle.Ficklefile;
using Fickle.Model;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Platform.VirtualFileSystem;

namespace Fickle.MsBuild
{
    public class FickleBuildTask : AppDomainIsolatedTask
	{
	    public string InputFile { get; set; }
		public string InputUrl { get; set; }

		[Required]
		public string Language { get; set; }

		public string Namespace { get; set; }

		public ITaskItem[] Includes { get; set; }

	    public ITaskItem[] MappedTypeAssemblies { get; set; }

		[Required]
		public string OutputDir { get; set; }

		public bool GenerateClasses { get; set; }
		public bool GenerateGateways { get; set; }
		public bool GenerateEnums { get; set; }
		public bool GeneratePod { get; set; }

		public string Name { get; set; }
		public string Homepage { get; set; }
		public string License { get; set; }
		public string Summary { get; set; }
		public string Author { get; set; }
		public string Version { get; set; }
	   
		public bool ImportDependenciesAsFramework { get; set; }
		public string PodspecSource { get; set; }
		public string PodspecSourceFiles { get; set; }

	    public FickleBuildTask()
	    {
		    this.GenerateClasses = true;
		    this.GenerateGateways = true;
		    this.GenerateEnums = true;
		    this.GeneratePod = true;
	    }

		public override bool Execute()
		{
		    this.GenerateFickle();

			return true;
	    }

		private void GenerateFickle()
		{
			ServiceModel serviceModel;
			using (var reader = this.OpenFickleInputStream())
			{
				serviceModel = FicklefileParser.Parse(reader);
			}

			var codeGenerationOptions = new CodeGenerationOptions
			{
				GenerateClasses = this.GenerateClasses,
				GenerateGateways = this.GenerateGateways,
				GenerateEnums = this.GenerateEnums,
				GeneratePod = this.GeneratePod
			};

			var defaultServiceModelInfo = codeGenerationOptions.ServiceModelInfo;
			var serviceModelInfo = new ServiceModelInfo();

			serviceModelInfo.Import(defaultServiceModelInfo);
			serviceModelInfo.Import(serviceModel.ServiceModelInfo);

			if (this.Author != null)
			{
				serviceModelInfo.Author = this.Author;
			}

			if (this.Name != null)
			{
				serviceModelInfo.Name = this.Name;
			}

			if (this.Summary != null)
			{
				serviceModelInfo.Summary = this.Summary;
			}

			if (this.Version != null)
			{
				serviceModelInfo.Version = this.Version;
			}

			if (this.Homepage != null)
			{
				serviceModelInfo.Homepage = this.Homepage;
			}

			if (this.License != null)
			{
				serviceModelInfo.License = this.License;
			}

			if (this.PodspecSource != null)
			{
				serviceModelInfo.ExtendedValues["podspec.source"] = this.PodspecSource;
			}

			if (this.PodspecSourceFiles != null)
			{
				serviceModelInfo.ExtendedValues["podspec.source_files"] = this.PodspecSourceFiles;
			}

			codeGenerationOptions.ServiceModelInfo = serviceModelInfo;

			codeGenerationOptions.Namespace = this.Namespace;
			codeGenerationOptions.GenerateClasses = this.GenerateClasses;
			codeGenerationOptions.GenerateGateways = this.GenerateGateways;
			codeGenerationOptions.GenerateEnums = this.GenerateEnums;
			codeGenerationOptions.GeneratePod = this.GeneratePod;
			codeGenerationOptions.ImportDependenciesAsFramework = this.ImportDependenciesAsFramework;

			if (this.Includes != null && this.Includes.Length > 0)
			{
				codeGenerationOptions.Includes = this.Includes.Select(x => x.ToString());
			}

			if (this.MappedTypeAssemblies != null && this.MappedTypeAssemblies.Length > 0)
			{
				codeGenerationOptions.MappedTypeAssemblies = this.MappedTypeAssemblies.Select(x => x.ToString());
			}

			var outputDir = FileSystemManager.Default.ResolveDirectory(this.OutputDir);

			outputDir.Create(true);

			using (var codeGenerator = ServiceModelCodeGenerator.GetCodeGenerator(this.Language, outputDir, codeGenerationOptions))
			{
				codeGenerator.Generate(serviceModel);
			}
		}

	    private TextReader OpenFickleInputStream()
	    {
		    if (!string.IsNullOrWhiteSpace(this.InputUrl))
		    {
			    var myHttpWebRequest = (HttpWebRequest) WebRequest.Create(this.InputUrl);
			    var myHttpWebResponse = (HttpWebResponse) myHttpWebRequest.GetResponse();
			    var receiveStream = myHttpWebResponse.GetResponseStream();

			    if (receiveStream == null)
			    {
				    throw new Exception("Response stream is null");
			    }

			    return new StreamReader(receiveStream, Encoding.GetEncoding("utf-8"));
		    }

			if (!string.IsNullOrWhiteSpace(this.InputFile))
			{
				return new StreamReader(this.InputFile);
			}

			throw new Exception("Either FileInput or UrlInput parameters must be set");
	    }
    }
}
