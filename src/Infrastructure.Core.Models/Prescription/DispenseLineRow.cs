namespace Infrastructure.Core.Models.Prescription;

/// <summary>One line of a dispensation: the prescription detail and how much of it leaves the stock.</summary>
public sealed record DispenseLineRow(Guid PrescriptionDetailCode, decimal Quantity);
