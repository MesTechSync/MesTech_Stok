namespace MesTech.Tests.Integration._Shared;

public static class MockPdfGenerator
{
    public static byte[] GenerateMinimalPdf(string invoiceNumber)
    {
        var content = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n" +
                     $"2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n" +
                     $"3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R/Contents 4 0 R>>endobj\n" +
                     $"4 0 obj<</Length {invoiceNumber.Length + 20}>>stream\nBT /F1 12 Tf 100 700 Td ({invoiceNumber}) Tj ET\nendstream\nendobj\n" +
                     $"%%EOF\n";
        return System.Text.Encoding.ASCII.GetBytes(content);
    }
}
