using Antlr4.Runtime;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.IO;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui.Storage;


namespace MyExcelMAUIApp3
{
    public partial class MainPage : ContentPage
    {
        const int CountColumn = 10;
        const int CountRow = 10;
        private Cell[,] cells;
        private List<List<Entry>> entryCells = new List<List<Entry>>();

        public MainPage()
        {
            InitializeComponent();
            cells = new Cell[CountRow, CountColumn];
            CreateGrid();
        }

        private void CreateGrid()
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int col = 1; col <= CountColumn; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int row = 1; row <= CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
        }

        private void AddColumnsAndColumnLabels()
        {
            for (int col = 1; col <= CountColumn; col++)
            {
                var label = new Label
                {
                    Text = GetColumnName(col),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(label, 0);
                Grid.SetColumn(label, col);
                grid.Children.Add(label);
            }
        }

        private void AddRowsAndCellEntries()
        {
            for (int row = 1; row <= CountRow; row++)
            {
                var rowLabel = new Label
                {
                    Text = row.ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center
                };
                Grid.SetRow(rowLabel, row);
                Grid.SetColumn(rowLabel, 0);
                grid.Children.Add(rowLabel);

                var rowEntries = new List<Entry>();

                for (int col = 1; col <= CountColumn; col++)
                {
                    var cell = new Cell(cells);
                    cells[row - 1, col - 1] = cell;

                    var entry = new Entry
                    {
                        Text = cell.Content,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        WidthRequest = 80, 
                        BackgroundColor = Microsoft.Maui.Graphics.Colors.White
                    };
                    entry.Unfocused += async (sender, e) => await Entry_UnfocusedAsync(sender, e, cell);

                    Grid.SetRow(entry, row);
                    Grid.SetColumn(entry, col);
                    grid.Children.Add(entry);
                    rowEntries.Add(entry);
                }
                entryCells.Add(rowEntries);
            }
        }

        private async Task Entry_UnfocusedAsync(object sender, FocusEventArgs e, Cell cell)
        {
            var entry = (Entry)sender;
            cell.Content = entry.Text;
        }

        private string GetColumnName(int colIndex)
        {
            int dividend = colIndex;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }

        private async void CalculateButton_Clicked(object sender, EventArgs e)
        {
            foreach (var cell in cells)
            {
                if (!string.IsNullOrWhiteSpace(cell.Content))
                {
                    await cell.EvaluateAsync(this);
                }
            }

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            for (int row = 0; row < CountRow; row++)
            {
                for (int col = 0; col < CountColumn; col++)
                {
                    var cell = cells[row, col];
                    var entry = entryCells[row][col];
                    entry.Text = cell.Value?.ToString() ?? string.Empty;
                }
            }
        }

        private void RefreshSavedGrid()
        {
            for (int row = 0; row < CountRow; row++)
            {
                for (int col = 0; col < CountColumn; col++)
                {
                    var cell = cells[row, col];
                    var entry = entryCells[row][col];
                    entry.Text = cell.Content;
                }
            }
        }


        private async void OpenButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                var file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Оберіть JSON файл",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.Android, new[] { "application/json" } },
                        { DevicePlatform.iOS, new[] { "public.json" } },
                        { DevicePlatform.WinUI, new[] { ".json" } },
                        { DevicePlatform.MacCatalyst, new[] { "public.json" } }
                    })
                });

                if (file == null)                
                    return;                

                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                string jsonContent = await reader.ReadToEndAsync();

                var cellContentsList = JsonSerializer.Deserialize<List<List<string>>>(jsonContent);
                if (cellContentsList == null)
                {
                    DisplayAlert("Помилка", "Не вдалося завантажити таблицю з файлу", "OK");
                    return;
                }

                for (int row = 0; row < cellContentsList.Count && row < CountRow; row++)
                {
                    for (int col = 0; col < cellContentsList[row].Count && col < CountColumn; col++)                    
                        cells[row, col].Content = cellContentsList[row][col];                    
                }

                RefreshSavedGrid(); 
                DisplayAlert("Відкриття", "Таблиця успішно завантажена з файлу", "OK");
            }
            catch (Exception ex)
            {
                DisplayAlert("Помилка", $"Не вдалося відкрити файл: {ex.Message}", "OK");
            }
        }


        private async void ExitButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Видійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                System.Environment.Exit(0);
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            var cellContentsList = new List<List<string>>();
            for (int row = 0; row < CountRow; row++)
            {
                var rowList = new List<string>();
                for (int col = 0; col < CountColumn; col++)
                    rowList.Add(cells[row, col].Content);

                cellContentsList.Add(rowList);
            }

            string json = JsonSerializer.Serialize(cellContentsList);

            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                try
                {
                    var result = await FileSaver.Default.SaveAsync("SpreadsheetData.json", stream);
                    if (result != null)
                        DisplayAlert("Збереження", "Таблиця успішно збережена", "OK");
 
                    else
                        DisplayAlert("Збереження", "Збереження скасовано користувачем", "OK");
                }
                catch (Exception ex)
                {
                    DisplayAlert("Помилка", $"Не вдалося зберегти файл: {ex.Message}", "OK");
                }
            }
        }

        private async void HelpButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Довідка", "Лабораторна робота 1. Прядко Ангеліна", "OK");
        }
    }

}
