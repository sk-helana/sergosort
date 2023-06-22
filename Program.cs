using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// See https://aka.ms/new-console-template for more information

void Process(string input, string output, bool cleanup = false)
{
    //Load and parse
    var content = File.ReadAllText(input);
    var doc = XDocument.Parse(content);

    //Sort attributes
    var c = StringComparer.Create(CultureInfo.InvariantCulture, true);
    Comparison<XAttribute?> comparer = (XAttribute? x, XAttribute? y) => c.Compare(x?.Name.LocalName, y?.Name.LocalName);
    foreach (var descendant in doc.Descendants().Where(x => x.HasAttributes))
    {
        var attributes = descendant.Attributes().ToList();
        if (cleanup)
        {
            var id = attributes.FirstOrDefault(a => a.Name.LocalName == "id");
            if (id != null) attributes.Remove(id);
        }

        attributes.Sort(comparer);
        descendant.ReplaceAttributes(attributes.ToArray());
    }

    //Sort elements
    var sort = true;
    while (sort)
    {
        sort = false;

        foreach (var element in doc.Descendants())
        {
            var next = element.ElementsAfterSelf().FirstOrDefault();
            if (next == null)
                continue;

            //ToString() includes any child elements so this will also sort <x>a</x> before <x>b</x>
            if (element.ToString().CompareTo(next.ToString()) > 0)
            {
                next.Remove();
                element.AddBeforeSelf(next);
                sort = true;
            }
        }
    }

    //Save
    doc.Save(output);
}

string errorInfo = "initializing";

try
{
    var input = args.FirstOrDefault(a => !a.StartsWith("-"));
    var recurse = args.Any(a => a == "-r" || a == "--recurse");
    var tolerant = args.Any(a => a == "-t" || a == "--tolerant");
    var cleanup = args.Any(a => a == "-id" || a == "--remove-id");

    //assume input if there is only one argument
    if (input == null && args.Length == 1)
        input = args[0];

    if (input == null)
    {
        Console.WriteLine("xmlsort 2.0 - Sergoifies xml files alphabetically");
        Console.WriteLine("Usage: xmlsort[.exe] <input> [-r] [-t]");

        Console.WriteLine(" -r, --recurse: Recursively search subdirectories");
        Console.WriteLine(" -t, --tolerant: Fault tolerant mode");
        Console.WriteLine(" -id, --remove-id: Remove all \"id\" attributes.");

        return 1;
    }

    var path = Path.GetDirectoryName(input);

    if (string.IsNullOrEmpty(path))
        path = Directory.GetCurrentDirectory();

    string[]? files = Directory.GetFiles(
        path,
        Path.GetFileName(input),
        recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    foreach (var file in files)
    {
        try
        {
            Console.WriteLine(file);
            errorInfo = $"processing {file}";
            Process(file, file, cleanup);
        }
        catch (Exception e)
        {
            if (!tolerant)
                throw;
            Console.WriteLine($"Errored while {errorInfo} ({e.GetType()}): {e.Message}");
        }
    }

    return 0;
}
catch (Exception e)
{
    Console.WriteLine($"Errored while {errorInfo} ({e.GetType()}): {e.Message}");
    return 1;
}