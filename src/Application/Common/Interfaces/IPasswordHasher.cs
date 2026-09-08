namespace ShippingSystem.Application.Common.Interfaces;

/// <summary>FR-1.1/1.2 — Customer/AdminUser/DeliveryAgent all store a PasswordHash; this is how it's produced/checked.</summary>
public interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string passwordHash);
}
