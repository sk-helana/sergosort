using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

// See https://aka.ms/new-console-template for more information

try
{
    if(args.Length < 2)
    {
        Console.WriteLine("sortmyxml 2.0 - Sergoifies xml files alphabetically");
        Console.WriteLine("Usage: sortmyxml[.exe] <input file> <output file>");
        return 1;
    }

    var input = args[0];
    var output = args[1];

    //Load and parse
    var content = File.ReadAllText(input);
    var doc = XDocument.Parse(content);

    //Sort attributes
    var c = StringComparer.Create(CultureInfo.InvariantCulture, true);
    Comparison<XAttribute?> comparer = (XAttribute? x, XAttribute? y) => c.Compare(x?.Name.LocalName, y?.Name.LocalName);
    foreach (var descendant in doc.Descendants().Where(x => x.HasAttributes))
    {
        var attributes = descendant.Attributes().ToList();
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

    return 0;
} catch(Exception e)
{
    Console.WriteLine($"Errored ({e.GetType()}): {e.Message}");
    return 1;
}