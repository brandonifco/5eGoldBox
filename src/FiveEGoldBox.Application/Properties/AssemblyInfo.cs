using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FiveEGoldBox.Application.Tests")]

// The console's tests construct combat view models directly to cover states the
// facade will not produce, such as a decision still awaiting automatic
// processing. Those types validate their own shape through internal
// constructors, so the test project needs the same access.
[assembly: InternalsVisibleTo("FiveEGoldBox.Console.Tests")]
