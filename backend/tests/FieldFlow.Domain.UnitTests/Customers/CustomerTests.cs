using FieldFlow.Domain.Customers;
using FieldFlow.Domain.UnitTests.TestData;

namespace FieldFlow.Domain.UnitTests.Customers;

public class CustomerTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Cnpj_accepts_valid_documents_formatted_or_not(string input)
    {
        var result = Cnpj.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("11222333000181");
        result.Value.Formatted.Should().Be("11.222.333/0001-81");
    }

    [Theory]
    [InlineData("11.222.333/0001-82")] // dígito verificador errado
    [InlineData("00000000000000")]     // todos iguais
    [InlineData("1122233300018")]      // 13 dígitos
    [InlineData(null)]
    public void Cnpj_rejects_invalid_documents(string? input)
    {
        Cnpj.Create(input).Error.Code.Should().Be("CUSTOMER_DOCUMENT_INVALID");
    }

    [Fact]
    public void Create_normalizes_email_and_phone()
    {
        var customer = Customer.Create("  Acme  ", "11.222.333/0001-81", "Contato@Acme.com.br", "(11) 98888-7777",
            DomainFixtures.Address()).Value;

        customer.Name.Should().Be("Acme");
        customer.Email.Should().Be("contato@acme.com.br");
        customer.Phone.Should().Be("11988887777");
    }

    [Theory]
    [InlineData("", "11.222.333/0001-81", "a@b.com", "1133334444", "CUSTOMER_NAME_REQUIRED")]
    [InlineData("Acme", "11.222.333/0001-81", "not-an-email", "1133334444", "CUSTOMER_EMAIL_INVALID")]
    [InlineData("Acme", "11.222.333/0001-81", "a@b.com", "123", "CUSTOMER_PHONE_INVALID")]
    [InlineData("Acme", "123", "a@b.com", "1133334444", "CUSTOMER_DOCUMENT_INVALID")]
    public void Create_validates_invariants(string name, string document, string email, string phone, string expectedCode)
    {
        Customer.Create(name, document, email, phone, DomainFixtures.Address()).Error.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("Rua A", "10", "Campinas", "S", "13000000")]
    [InlineData("Rua A", "10", "Campinas", "SP", "1300")]
    [InlineData("", "10", "Campinas", "SP", "13000000")]
    public void Address_validates_fields(string street, string number, string city, string state, string zip)
    {
        Address.Create(street, number, city, state, zip).Error.Code.Should().Be("CUSTOMER_ADDRESS_INVALID");
    }

    [Fact]
    public void Address_normalizes_state_and_zip()
    {
        var address = Address.Create("Rua A", "10", "Campinas", "sp", "13000-000").Value;

        address.State.Should().Be("SP");
        address.ZipCode.Should().Be("13000000");
    }
}
