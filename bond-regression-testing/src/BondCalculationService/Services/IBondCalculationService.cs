using BondCalculationService.Models;

namespace BondCalculationService.Services;

/// <summary>
/// Interface for bond yield calculations.
/// In production, the implementation wraps a third-party quant library.
/// </summary>
public interface IBondCalculationService
{
    /// <summary>
    /// Calculate yield-to-maturity and related analytics for a bond.
    /// </summary>
    /// <param name="bond">Bond parameters (coupon, maturity, etc.)</param>
    /// <param name="price">Market price of the bond</param>
    /// <param name="settlementDate">Settlement date for the calculation</param>
    /// <returns>Calculated yield and analytics</returns>
    YieldResult CalculateYield(BondParameters bond, decimal price, DateOnly settlementDate);
}
