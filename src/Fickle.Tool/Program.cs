﻿using System;
using System.Collections.Generic;
using System.IO;
using Fickle.Ficklefile;
using Fickle.Model;
using Platform.IO;
using Platform.VirtualFileSystem;

namespace Fickle.Tool
{
	class Program
	{
		static int Main(string[] args)
		{
			ServiceModel serviceModel;
			var options = new CommandLineOptions();

			if (!CommandLine.Parser.Default.ParseArguments(args, options))
			{
				Console.Error.WriteLine("Unable to parse command line arguments");

				return 1;
			}

			if (options.Input == null)
			{
				Console.Error.WriteLine("Must specify input file");

				return 1;
			}

			TextReader textReader = null;
			
			foreach (var url in options.Input)
			{
				var currentUrl = url;

				if (currentUrl.IndexOf(":", StringComparison.Ordinal) <= 0)
				{
					currentUrl = "./" + url;
				}
				
				var reader = FileSystemManager.Default.ResolveFile(currentUrl).GetContent().GetReader(FileShare.Read);

				textReader = textReader == null ? reader : textReader.Concat(reader);
			}
			
			if (!string.IsNullOrEmpty(options.Output) && options.Output.IndexOf(":", StringComparison.Ordinal) <= 0)
			{
				options.Output = "./" + options.Output;
			}

			using (var reader = textReader)
			{
				serviceModel = FicklefileParser.Parse(reader);
			}

			object outputObject = Console.Out;

			if (!string.IsNullOrEmpty(options.Output))
			{
				var dir = FileSystemManager.Default.ResolveDirectory(options.Output);

				dir.Create(true);

				outputObject = dir;
			}

			var codeGenerationOptions = new CodeGenerationOptions();
			var defaultServiceModelInfo = codeGenerationOptions.ServiceModelInfo;
			var serviceModelInfo = new ServiceModelInfo();

			serviceModelInfo.Import(defaultServiceModelInfo);
			serviceModelInfo.Import(serviceModel.ServiceModelInfo);
			
			if (options.Author != null)
			{
				serviceModelInfo.Author = options.Author;
			}

			if (options.Name != null)
			{
				serviceModelInfo.Name = options.Name;
			}

			if (options.Summary != null)
			{
				serviceModelInfo.Summary = options.Summary;
			}

			if (options.Version != null)
			{
				serviceModelInfo.Version = options.Version;
			}

			if (options.Homepage != null)
			{
				serviceModelInfo.Homepage = options.Homepage;
			}

			if (options.License != null)
			{
				serviceModelInfo.License = options.License;
			}

			if (options.PodspecSource != null)
			{
				serviceModelInfo.ExtendedValues["podspec.source"] = options.PodspecSource;
			}

			if (options.PodspecSourceFiles != null)
			{
				serviceModelInfo.ExtendedValues["podspec.source_files"] = options.PodspecSourceFiles;
			}

			codeGenerationOptions.GeneratePod = options.Pod;
			codeGenerationOptions.ImportDependenciesAsFramework = options.ImportDependenciesAsFramework;
			codeGenerationOptions.ServiceModelInfo = serviceModelInfo;

			using (var codeGenerator = ServiceModelCodeGenerator.GetCodeGenerator(options.Language, outputObject, codeGenerationOptions))
			{
				codeGenerator.Generate(serviceModel);
			}

			return 0;
		}
	}
}
