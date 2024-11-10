using Antlr4.Runtime;

namespace MyExcelMAUIApp3
{
    public class Cell
    {
        public string Content { get; set; }
        public double? Value { get; private set; }
        public bool IsEmpty = false;
        public HashSet<Cell> ReferencedBy { get; set; }
        public HashSet<Cell> References { get; set; }

        private readonly Cell[,] cells;

        public Cell(Cell[,] cells)
        {
            Content = string.Empty;
            ReferencedBy = new HashSet<Cell>();
            References = new HashSet<Cell>();
            this.cells = cells;
        }

        public void AddReference(Cell cell)
        {
            References.Add(cell);
            cell.ReferencedBy.Add(this);

            foreach (var reference in cell.References)
            {
                References.Add(reference);
            }
        }

        public bool CheckForCyclicReference(Cell targetCell)
        {
            Stack<Cell> stack = new Stack<Cell>();
            stack.Push(targetCell);

            while (stack.Count > 0)
            {
                var cell = stack.Pop();
                if (cell == this)
                {
                    return true;
                }

                foreach (var reference in cell.References)
                {
                    stack.Push(reference);
                }
            }
            return false;
        }

        public void ClearReferencesAndContent()
        {
            Content = string.Empty;
            Value = null;

        }

        public async Task EvaluateAsync(ContentPage page)
        {
            if (string.IsNullOrWhiteSpace(Content))
            {
                Value = 0;
                return;
            }

            try
            {
                var inputStream = new AntlrInputStream(Content);
                var lexer = new GrammarLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);
                var parser = new GrammarParser(tokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new CustomErrorListener(page, this));

                var context = parser.compileUnit();

                if (parser.NumberOfSyntaxErrors == 0)
                {
                    var evaluator = new SpreadsheetEvaluator(cells, this, page);
                    Value = evaluator.Visit(context);
                }
                else
                {
                    Content = string.Empty;
                    Value = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка обробки виразу: {ex.Message}");
                Value = null;
            }

            if (IsEmpty)
                ClearReferencesAndContent();
        }
    }
}