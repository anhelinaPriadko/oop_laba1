using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MyExcelMAUIApp3
{
    public class SpreadsheetEvaluator : GrammarBaseVisitor<double>
    {
        private readonly Cell[,] cells;
        private readonly Cell currentCell;
        private readonly ContentPage page;

        public SpreadsheetEvaluator(Cell[,] cells, Cell currentCell, ContentPage page)
        {
            this.cells = cells;
            this.currentCell = currentCell;
            this.page = page;
        }

        public override double VisitCompileUnit(GrammarParser.CompileUnitContext context)
        {
            return Visit(context.expression());
        }

        public override double VisitParenthesizedExpr(GrammarParser.ParenthesizedExprContext context)
        {
            double result = Visit(context.expression());
            return result;
        }

        public override double VisitUnaryExpr(GrammarParser.UnaryExprContext context)
        {
            double result = Visit(context.expression());

            if (context.operatorToken.Type == GrammarParser.ADD)
            {
                return result;
            }
            else if (context.operatorToken.Type == GrammarParser.SUBTRACT)
            {
                double negatedResult = -result;
                return negatedResult;
            }

            throw new InvalidOperationException("Невідомий унарний оператор");
        }


        public override double VisitNumberExpr(GrammarParser.NumberExprContext context)
        {
            var numberText = context.NUMBER().GetText();
            double result = double.Parse(numberText);
            return result;
        }

        public override double VisitAdditiveExpr(GrammarParser.AdditiveExprContext context)
        {
            double left = Visit(context.expression(0));
            double right = Visit(context.expression(1));
            double result = context.operatorToken.Type == GrammarParser.ADD ? left + right : left - right;

            return result;
        }

        public override double VisitMultiplicativeExpr(GrammarParser.MultiplicativeExprContext context)
        {
            double left = Visit(context.expression(0));
            double right = Visit(context.expression(1));

            if (right == 0 && (context.operatorToken.Type == GrammarParser.DIVIDE ||
                                context.operatorToken.Type == GrammarParser.DIV ||
                                context.operatorToken.Type == GrammarParser.MOD))
            {
                page.Dispatcher.Dispatch(async () =>
                {
                    await page.DisplayAlert("Помилка", "Виконується ділення на 0!", "OK");
                    currentCell.IsEmpty = true;
                });


                currentCell.IsEmpty = true;
                return 0.0;
            }

            double result = context.operatorToken.Type switch
            {
                GrammarParser.MULTIPLY => left * right,
                GrammarParser.DIVIDE => Math.Round(left / right, 3),
                GrammarParser.DIV => Math.Floor(left / right),
                GrammarParser.MOD => left % right,
                _ => throw new InvalidOperationException("Невідомий оператор")
            };
            return result;
        }


        private int ConvertColumnToIndex(string column)
        {
            int index = 0;
            for (int i = 0; i < column.Length; i++)
            {
                index *= 26;
                index += column[i] - 'A' + 1;
            }

            return index - 1;
        }

        public override double VisitCellReferenceExpr(GrammarParser.CellReferenceExprContext context)
        {
            string cellRef = context.CELLREF().GetText();
            var column = new string(cellRef.TakeWhile(char.IsLetter).ToArray());
            var row = int.Parse(new string(cellRef.SkipWhile(char.IsLetter).ToArray()));

            int columnIndex = ConvertColumnToIndex(column);
            int rowIndex = row - 1;

            if (rowIndex < 0 || rowIndex >= cells.GetLength(0) || columnIndex < 0 || columnIndex >= cells.GetLength(1))
            {
                page.Dispatcher.Dispatch(async () =>
                {
                    await page.DisplayAlert("Помилка", "Використано клітинку, що є поза межами таблиці!", "OK");
                    currentCell.IsEmpty = true;
                });

                return 0.0;
            }

            var referencedCell = cells[rowIndex, columnIndex];

            currentCell.References.Add(referencedCell);

            foreach (var cell in referencedCell.References)
                currentCell.References.Add(cell);

            if (referencedCell != null)
            {
                if (currentCell.CheckForCyclicReference(referencedCell))
                {

                    Debug.WriteLine(currentCell.References.Count());

                    foreach (var cell in currentCell.References)
                    {
                        cell.ClearReferencesAndContent();
                        Debug.WriteLine(cell.Value);
                        Debug.WriteLine(cell.Content);
                    }

                    referencedCell.ClearReferencesAndContent();
                    currentCell.ClearReferencesAndContent();

                    page.Dispatcher.Dispatch(async () =>
                    {
                        await page.DisplayAlert("Помилка", "Виявлено циклічне посилання!", "OK");
                    });

                }
            }

            double cellValue;

            if (referencedCell != null && referencedCell.Value.HasValue)
            {
                cellValue = referencedCell.Value.Value;
            }
            else
            {
                cellValue = 0.0;
                currentCell.IsEmpty = true;
            }

            return cellValue;
        }
    }
}