using System.Reflection;

var asm = typeof(Square.SquareClient).Assembly;
Console.WriteLine($"Square assembly: {asm.FullName}");

static void Dump(string title, IEnumerable<string> items)
{
    Console.WriteLine("\n== " + title + " ==");
    foreach (var i in items.OrderBy(x => x))
        Console.WriteLine(i);
}

var types = asm.GetTypes();

Dump("Top namespaces", types.Select(t => t.Namespace).Where(n => n is not null)!.Distinct().Select(n => n!));

Dump("SquareClient members", typeof(Square.SquareClient).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(m => m.ToString()!));

var envType = asm.GetTypes().FirstOrDefault(t => t.FullName == "Square.Environment" || t.FullName == "Square.SquareEnvironment" || t.Name.Contains("Environment"));
Console.WriteLine($"\nEnvironment-like type: {envType?.FullName ?? "<not found>"}");

var moneyType = types.FirstOrDefault(t => t.Name == "Money");
Console.WriteLine($"Money type: {moneyType?.FullName ?? "<not found>"}");

var createPaymentLinkReq = types.FirstOrDefault(t => t.Name.Contains("CreatePaymentLinkRequest"));
Console.WriteLine($"CreatePaymentLinkRequest type: {createPaymentLinkReq?.FullName ?? "<not found>"}");

var apiException = types.FirstOrDefault(t => t.Name.Contains("ApiException") || t.Name.Contains("ApiError"));
Console.WriteLine($"ApiException-like type: {apiException?.FullName ?? "<not found>"}");

static void DumpCtors(Type t)
{
    Console.WriteLine($"\n-- {t.FullName} ctors --");
    foreach (var c in t.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        Console.WriteLine(c);
}

static void DumpProps(Type t)
{
    Console.WriteLine($"\n-- {t.FullName} props --");
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        Console.WriteLine(p);
}

static void DumpMethods(Type t, string contains)
{
    Console.WriteLine($"\n-- {t.FullName} methods (contains '{contains}') --");
    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => m.Name.Contains(contains, StringComparison.OrdinalIgnoreCase)))
        Console.WriteLine(m);
}

var clientOptionsType = asm.GetType("Square.ClientOptions");
if (clientOptionsType is not null)
{
    DumpCtors(clientOptionsType);
    DumpProps(clientOptionsType);
}

if (moneyType is not null)
{
    DumpCtors(moneyType);
    DumpProps(moneyType);
}

var quickPayType = asm.GetType("Square.Checkout.PaymentLinks.QuickPay");
if (quickPayType is not null)
{
    DumpCtors(quickPayType);
    DumpProps(quickPayType);
}

if (createPaymentLinkReq is not null)
{
    DumpCtors(createPaymentLinkReq);
    DumpProps(createPaymentLinkReq);
}

var paymentLinksClientType = asm.GetType("Square.Checkout.PaymentLinks.PaymentLinksClient");
if (paymentLinksClientType is not null)
{
    DumpMethods(paymentLinksClientType, "PaymentLink");
    DumpMethods(paymentLinksClientType, "Create");
}
