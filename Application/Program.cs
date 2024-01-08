
using CDPStudio;
using CDPStudio.Models;

class Program
{
    public static void Main()
    {
        Client client = new Client("ws://10.5.8.100:7689");
        client.Connect();

        Console.WriteLine($"Connected to: {client.Metadata()!.ApplicationName}");

        Task.Run(async () =>
        {
            TreeNode node = await client.Find("This.Is.A.Litte.Test");
        });

        Console.ReadLine();
    }
}

