namespace Mango.Services.OrderAPI.Models.Dto
{
    public class DailyIncomeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalIncome { get; set; }
    }
}
