using MiniExcelLibs;
using Readhtpk.Models;
using Readhtpk.Models.Import;

namespace Readhtpk.Helpers
{
    public static class ExcelImportHelper
    {
        public static async Task<(List<Question> questions, List<QuestionImportResult> results)>
            ImportQuestionsAsync(Stream stream, int subjectId)
        {
            var results = new List<QuestionImportResult>();
            var questions = new List<Question>();

            try
            {
                // Đọc Excel - MiniExcel sẽ đọc theo cột A, B, C...
                var rows = await MiniExcel.QueryAsync(stream);
                var rowsList = rows.ToList();

                if (rowsList.Count == 0)
                {
                    throw new Exception("File Excel không có dữ liệu");
                }

                // Bỏ qua dòng header (dòng 1), bắt đầu từ dòng 2
                var dataRows = rowsList.Skip(1);

                int rowNum = 2; // Bắt đầu từ dòng 2 trong Excel

                foreach (dynamic row in dataRows)
                {
                    var result = new QuestionImportResult { RowNumber = rowNum };

                    try
                    {
                        var rowDict = row as IDictionary<string, object>;
                        if (rowDict == null)
                        {
                            throw new Exception("Không thể đọc dòng dữ liệu");
                        }

                        // Helper function để lấy giá trị theo tên cột A, B, C...
                        string GetVal(string columnKey)
                        {
                            if (rowDict.TryGetValue(columnKey, out var val) && val != null)
                            {
                                var str = val.ToString()?.Trim();
                                return string.IsNullOrEmpty(str) ? "" : str;
                            }
                            return "";
                        }

                        // Lấy giá trị theo cột (A=Content, B=AnswerA, C=AnswerB...)
                        var content = GetVal("A");
                        if (string.IsNullOrEmpty(content))
                        {
                            throw new Exception("Nội dung câu hỏi không được để trống (Cột A)");
                        }

                        var correctAnswer = GetVal("F").ToUpper(); // Cột F = CorrectAnswer
                        if (string.IsNullOrEmpty(correctAnswer) || !"ABCD".Contains(correctAnswer))
                        {
                            throw new Exception("Đáp án đúng phải là A, B, C hoặc D (Cột F)");
                        }

                        var question = new Question
                        {
                            SubjectId = subjectId,
                            Content = content,
                            AnswerA = GetVal("B"), // Cột B
                            AnswerB = GetVal("C"), // Cột C
                            AnswerC = GetVal("D"), // Cột D
                            AnswerD = GetVal("E"), // Cột E
                            CorrectAnswer = correctAnswer,
                            Explanation = GetVal("G"), // Cột G
                            Difficulty = int.TryParse(GetVal("H"), out int d) && d >= 1 && d <= 3 ? d : 1, // Cột H
                            IsActive = bool.TryParse(GetVal("I"), out bool isActive) ? isActive : true, // Cột I
                            CreatedAt = DateTime.Now
                        };

                        questions.Add(question);
                        result.Content = content;
                        result.IsSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        result.Content = $"Dòng {rowNum}";
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                    }

                    results.Add(result);
                    rowNum++;
                }
            }
            catch (Exception ex)
            {
                results.Add(new QuestionImportResult
                {
                    RowNumber = 0,
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi đọc file Excel: {ex.Message}"
                });
            }

            return (questions, results);
        }
    }
}