using System.Runtime.CompilerServices;

// The vendor path under test, below the narration source rule (the own-voice change, decision D-002 of its design): the
// produce service's internal entry that names a boundary source explicitly is visible to the tests that drive it, and to
// no production assembly.
[assembly: InternalsVisibleTo("MediaCompany.Persistence.Tests")]
[assembly: InternalsVisibleTo("MediaCompany.Production.Tests")]
