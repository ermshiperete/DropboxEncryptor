using System;
using System.Diagnostics;
using DropboxEncryptorTests;
using NUnitLite;

namespace TestRunner
{
	class Program
	{
		static void Main(string[] args)
		{
			//var autoRun = new AutoRun();
			var assembly = typeof(FileHandlerWatcherTests).Assembly;
			new AutoRun(assembly).Execute(args);
		}
	}
}