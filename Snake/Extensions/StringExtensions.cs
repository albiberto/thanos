namespace Snakes.Core.Extensions;

using Spectre.Console;
using Spectre.Console.Testing;

public static class StringExtensions
{
    extension(string self)
    {
        public string WithStyle(Style style)
            => $"[{style.ToMarkup()}]{self}[/]";

        public string ToAnsi()
        {
            var console = new TestConsole
            {
                EmitAnsiSequences = true
            };
        
            console.Write(new Markup(self));
            
            return console.Output;
        }
    }
}