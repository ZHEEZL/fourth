using System.Globalization;
using System.Text;

namespace SortingLab.Core.External;

/// <summary>Одна строка (запись) таблицы из CSV-файла.</summary>
public sealed record TableRecord(int Id, IReadOnlyList<string> Values)
{
    public string GetValue(int columnIndex) =>
        columnIndex >= 0 && columnIndex < Values.Count ? Values[columnIndex] : "";
}

/// <summary>Сравнение записей таблицы по выбранному столбцу (числа или русский алфавит).</summary>
public sealed class RecordKeyComparer : IComparer<TableRecord>
{
    private static readonly StringComparer TextComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

    public RecordKeyComparer(int keyColumnIndex, string keyColumnName, bool isNumeric)
    {
        KeyColumnIndex = keyColumnIndex;
        KeyColumnName = keyColumnName;
        IsNumeric = isNumeric;
    }

    public int KeyColumnIndex { get; }
    public string KeyColumnName { get; }
    public bool IsNumeric { get; }

    public int Compare(TableRecord? x, TableRecord? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        string sx = x.GetValue(KeyColumnIndex);
        string sy = y.GetValue(KeyColumnIndex);

        if (IsNumeric && CsvTable.TryParseNumber(sx, out double nx) && CsvTable.TryParseNumber(sy, out double ny))
            return nx.CompareTo(ny);

        return TextComparer.Compare(sx, sy);
    }

    public string FormatKey(TableRecord record) => record.GetValue(KeyColumnIndex);
}

/// <summary>Таблица с произвольным числом столбцов, загруженная из CSV.</summary>
public sealed class CsvTable
{
    public CsvTable(string name, IReadOnlyList<string> headers, IReadOnlyList<TableRecord> records, char delimiter = ',')
    {
        Name = name;
        Headers = headers;
        Records = records;
        Delimiter = delimiter;
    }

    public string Name { get; }
    public IReadOnlyList<string> Headers { get; }
    public IReadOnlyList<TableRecord> Records { get; }
    public char Delimiter { get; }

    /// <summary>Загружает CSV-таблицу из файла на диске.</summary>
    public static CsvTable LoadFromFile(string path)
    {
        string text = File.ReadAllText(path, Encoding.UTF8);
        return Parse(text, Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>Разбирает текст CSV с автоопределением разделителя «;» или «,».</summary>
    public static CsvTable Parse(string content, string name = "Таблица")
    {
        ArgumentNullException.ThrowIfNull(content);

        // Убираем UTF-8 BOM, если он есть в начале файла.
        string clean = content.TrimStart('\uFEFF');
        var lines = SplitLines(clean);
        if (lines.Count == 0)
            throw new FormatException("Файл пуст: в таблице нет ни одной строки.");

        char delimiter = DetectDelimiter(lines[0]);
        var headers = ParseCsvLine(lines[0], delimiter);
        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
            throw new FormatException("В первой строке файла не найдены заголовки столбцов.");

        for (int i = 0; i < headers.Count; i++)
            headers[i] = string.IsNullOrWhiteSpace(headers[i]) ? $"Поле {i + 1}" : headers[i].Trim();

        var records = new List<TableRecord>(Math.Max(0, lines.Count - 1));
        for (int i = 1; i < lines.Count; i++)
        {
            var cells = ParseCsvLine(lines[i], delimiter);
            while (cells.Count < headers.Count)
                cells.Add("");
            if (cells.Count > headers.Count)
                cells.RemoveRange(headers.Count, cells.Count - headers.Count);

            for (int c = 0; c < cells.Count; c++)
                cells[c] = cells[c].Trim();

            records.Add(new TableRecord(i, cells));
        }

        return new CsvTable(name, headers, records, delimiter);
    }

    /// <summary>Отбирает записи по точному совпадению значения в указанном столбце.</summary>
    public IReadOnlyList<TableRecord> Filter(int? filterColumnIndex, string? filterValue)
    {
        if (filterColumnIndex is null || string.IsNullOrWhiteSpace(filterValue))
            return Records;

        int col = filterColumnIndex.Value;
        string target = filterValue.Trim();
        return Records
            .Where(r => string.Equals(r.GetValue(col), target, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>Уникальные непустые значения столбца для выпадающего списка фильтра.</summary>
    public IReadOnlyList<string> GetDistinctValues(int columnIndex) =>
        Records
            .Select(r => r.GetValue(columnIndex))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true))
            .Order(StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true))
            .ToArray();

    /// <summary>Создаёт компаратор для выбранного ключевого столбца.</summary>
    public RecordKeyComparer CreateComparer(int keyColumnIndex, IReadOnlyList<TableRecord>? subset = null)
    {
        var source = subset ?? Records;
        bool isNumeric = IsNumericColumn(keyColumnIndex, source);
        return new RecordKeyComparer(keyColumnIndex, Headers[keyColumnIndex], isNumeric);
    }

    /// <summary>Подбирает короткую подпись для карточки записи (обычно название страны или вещества).</summary>
    public string GetSubtitle(TableRecord record, int keyColumnIndex)
    {
        if (Headers.Count <= 1)
            return "";

        for (int i = 0; i < Headers.Count; i++)
        {
            if (i == keyColumnIndex)
                continue;
            string h = Headers[i];
            if (h.Contains("назван", StringComparison.OrdinalIgnoreCase) ||
                h.Contains("имя", StringComparison.OrdinalIgnoreCase) ||
                h.Contains("name", StringComparison.OrdinalIgnoreCase))
            {
                return record.GetValue(i);
            }
        }

        int fallback = keyColumnIndex == 0 ? 1 : 0;
        return record.GetValue(fallback);
    }

    /// <summary>Короткое имя записи для лога и пояснений: «Франция (68000000)» или «арбуз».</summary>
    public string DescribeRecord(TableRecord record, int keyColumnIndex)
    {
        string key = record.GetValue(keyColumnIndex);
        string sub = GetSubtitle(record, keyColumnIndex);
        return string.IsNullOrEmpty(sub) ? $"«{key}»" : $"«{sub}» ({key})";
    }

    /// <summary>Полное описание всех полей записи для всплывающей подсказки.</summary>
    public string FormatTooltip(TableRecord record)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Headers.Count; i++)
        {
            if (i > 0) sb.AppendLine();
            sb.Append(Headers[i]).Append(": ").Append(record.GetValue(i));
        }
        return sb.ToString();
    }

    /// <summary>Форматирует одну запись в строку CSV.</summary>
    public string FormatRecordLine(TableRecord record) =>
        string.Join(Delimiter, record.Values.Select(v => EscapeCsvCell(v, Delimiter)));

    /// <summary>Собирает полную CSV-таблицу из заголовков и указанного набора записей.</summary>
    public string ToCsv(IEnumerable<TableRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Delimiter, Headers.Select(h => EscapeCsvCell(h, Delimiter))));
        foreach (var record in records)
            sb.AppendLine(FormatRecordLine(record));
        return sb.ToString();
    }

    /// <summary>Записывает отсортированные записи в результирующий CSV-файл (UTF-8 с BOM для Excel).</summary>
    public void SaveToFile(string path, IEnumerable<TableRecord> records)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, ToCsv(records), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public static bool IsNumericColumn(int columnIndex, IReadOnlyList<TableRecord> records)
    {
        bool sawAny = false;
        foreach (var record in records)
        {
            string value = record.GetValue(columnIndex);
            if (string.IsNullOrWhiteSpace(value))
                continue;
            sawAny = true;
            if (!TryParseNumber(value, out _))
                return false;
        }
        return sawAny;
    }

    public static bool TryParseNumber(string text, out double value)
    {
        string normalized = text.Trim().Replace(" ", "").Replace('\u00A0', ' ').Replace(" ", "").Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static char DetectDelimiter(string headerLine)
    {
        int semicolons = CountOutsideQuotes(headerLine, ';');
        int commas = CountOutsideQuotes(headerLine, ',');
        return semicolons > commas ? ';' : ',';
    }

    private static int CountOutsideQuotes(string line, char target)
    {
        bool inQuotes = false;
        int count = 0;
        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (!inQuotes && c == target) count++;
        }
        return count;
    }

    private static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }
        return lines;
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static string EscapeCsvCell(string cell, char delimiter)
    {
        if (!cell.Contains(delimiter) && !cell.Contains('"') && !cell.Contains('\n') && !cell.Contains('\r'))
            return cell;

        return $"\"{cell.Replace("\"", "\"\"")}\"";
    }
}