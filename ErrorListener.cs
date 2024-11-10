using Antlr4.Runtime;


namespace MyExcelMAUIApp3
{
    public class CustomErrorListener : BaseErrorListener
    {
        public readonly ContentPage page;
        public Cell currentCell;

        public CustomErrorListener(ContentPage page, Cell currentCell) : base()
        {
            this.page = page;
            this.currentCell = currentCell;
        }

        public override void SyntaxError(
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            page.Dispatcher.Dispatch(async () =>
            {
                await page.DisplayAlert("Помилка синтаксису:", $"рядок {line}, позиція {charPositionInLine}: {msg}", "OK");
                currentCell.IsEmpty = true;
            });
        }
    }
}