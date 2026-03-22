using Readhtpk.Models;

namespace Readhtpk.Services
{
    public interface IQuestionService
    {
        Task<int> ImportQuestionsFromExcelAsync(Stream fileStream, int subjectId);
        Task<List<Question>> GetQuestionsBySubjectIdAsync(int subjectId);
        Task<Question?> GetQuestionByIdAsync(int id);
        Task<bool> CreateQuestionAsync(Question question);
        Task<bool> UpdateQuestionAsync(Question question);
        Task<bool> DeleteQuestionAsync(int id);
    }
}