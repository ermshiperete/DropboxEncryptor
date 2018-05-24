using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace DropboxEncryptor
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class Options
	{
		[Option('d', "daemon", HelpText = "Run as daemon")]
		public bool IsDaemon { get; set; }
		
		[Option("stop", HelpText = "Shutdown daemon")]
		public bool Stop { get; set; }
		
		[Option("status", HelpText = "Query status of daemon")]
		public bool Status { get; set; }

		public static ParserResult<Options> ParseCommandLineArgs(IEnumerable<string> args)
		{
			var parser = ParserInstance ?? Parser.Default;
			return parser.ParseArguments<Options>(args);
		}

		/// <summary>
		/// Gets or sets the parser.
		/// </summary>
		/// <remarks>Used in tests. If not set the default parser is used.</remarks>
		public static Parser ParserInstance { get; set; }
	}
}