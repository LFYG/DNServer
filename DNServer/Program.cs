﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Server;
using CommandLine;

namespace DNServer
{
	internal class Program
	{
		private class Options
		{
			// Omitting long name, defaults to name of property, ie "--verbose"
			[Option(Default = false, HelpText = @"Prints all messages to standard output.")]
			public bool Verbose { get; set; }

			[Option('b', @"bindip", HelpText = @"Listen on...", Default = @"0.0.0.0:53")]
			public string BindIpEndPoint { get; set; }

			[Option('u', @"updns", HelpText = @"Up DNS Server", Default = @"114.114.114.114:53")]
			public string UpDNS { get; set; }

			[Option('p', @"puredns", HelpText = @"Pure DNS Server", Default = null)]
			public string PureDNS { get; set; }

			[Option('l', @"list", HelpText = @"Domains list file path", Default = @"chndomains.txt")]
			public string Path { get; set; }
		}
		/* Pure DNS Server List
		 * 101.6.6.6:53
		 * 202.141.162.123:53
		 * 223.113.97.99:53
		 * 208.67.222.222:5353
		 * 208.67.220.220:443
		 */
		public static bool Verbose = false;
		public const int DNSDefaultPort = 53;
		private const int ListenDefaultPort = DNSDefaultPort;

		public static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args)
			.WithParsed(opts => RunOptionsAndReturnExitCode(opts))
			.WithNotParsed(HandleParseError);
		}

		private static int RunOptionsAndReturnExitCode(Options options)
		{
			Verbose = options.Verbose;
			IPEndPoint[] updns;
			IPEndPoint[] puredns;
			IPEndPoint bindIpEndPoint;
			try
			{
				updns = Common.ToIPEndPoints(options.UpDNS, 53, new[] { ',', '，' }) as IPEndPoint[];
				Console.WriteLine($@"UpDNS:{Common.FromIPEndPoints(updns)}");
				puredns = Common.ToIPEndPoints(options.PureDNS, 53, new[] { ',', '，' }) as IPEndPoint[];
				if (puredns != null)
				{
					Console.WriteLine($@"PureDNS:{Common.FromIPEndPoints(puredns)}");
				}
				bindIpEndPoint = Common.ToIPEndPoint(options.BindIpEndPoint, 53);
				Console.WriteLine($@"Listen on:{bindIpEndPoint}");
			}
			catch
			{
				Console.WriteLine(@"DNS Server ERROR!");
				return 1;
			}
			var path = options.Path;
			StartDNServer_Async(updns, puredns, path, bindIpEndPoint).Wait();
			return 0;
		}

		private static void HandleParseError(IEnumerable<Error> errs)
		{
		}

		private static async Task StartDNServer_Async(IPEndPoint[] updns, IPEndPoint[] puredns, string path)
		{
			var any = new IPEndPoint(IPAddress.Any, ListenDefaultPort);
			await StartDNServer_Async(updns, puredns, path, any);
		}
		private static async Task StartDNServer_Async(IPEndPoint[] updns, IPEndPoint[] puredns, string path, IPEndPoint bindipPoint)
		{
			var server = new DnsServer(new ApartRequestResolver(updns, puredns, path));

			if (Verbose)
			{
				server.Requested += (request) => Console.WriteLine($@"Requested: {request}");
				server.Responded += (request, response) => Console.WriteLine($@"Responded: {request} => {response}");
			}
			server.Listening += () => Console.WriteLine(@"Listening:");
			server.Errored += (e) =>
			{
				Console.WriteLine($@"Errored: {e}");
				if (e is ResponseException responseError)
				{
					Console.WriteLine(responseError.Response);
				}
			};
			await server.Listen(bindipPoint);
		}
	}
}