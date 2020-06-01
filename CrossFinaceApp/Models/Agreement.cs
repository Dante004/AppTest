namespace CrossFinaceApp.Models
{
    public class Agreement
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public int? PersonId { get; set; }
        public virtual Person Person { get; set; }
        public int? FinancialStateId { get; set; }
        public virtual FinancialState FinancialState { get; set; }
    }
}
