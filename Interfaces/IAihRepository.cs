using AvecADeskApi.Model.Aih;

namespace AvecADeskApi.Interfaces;

public interface IAihRepository
{
    Task<int> SubmitAihFormAsync(AihSubmitRequest request);
    Task<List<AihResponse>> GetAihInterestsAsync(int? vendorId, int? instituteId);
    Task<AihConvertResponse?> ConvertAihToStudentAsync(int interestId);
}
