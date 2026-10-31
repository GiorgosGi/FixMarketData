using FixMarketData.Simulator;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

var settings = new SessionSettings("acceptor.cfg");
var application = new SimulatorApplication();
var storeFactory = new FileStoreFactory(settings);
var logFactory = new FileLogFactory(settings);
using var acceptor = new ThreadedSocketAcceptor(application, storeFactory, settings, logFactory);

acceptor.Start();
Console.WriteLine("FIX simulator started on port 5001. Press ENTER to stop...");
Console.ReadLine();
acceptor.Stop();
