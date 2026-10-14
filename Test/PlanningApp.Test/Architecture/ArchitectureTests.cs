using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using MediatR;
using PlanningApp.Application.Common.Helpers;
using PlanningApp.Application.Common.Interfaces;
using PlanningApp.Domain.Tasks;
using Xunit;

namespace PlanningApp.Test.Architecture;

// Katman sınırlarını ve Domain kurallarını reflection ile doğrular (bkz. test_rules.md → Mimari Testler)
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(PlanningTask).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(WeekCalendar).Assembly;

    // ---- Bağımlılık yönü ----

    [Fact]
    public void Domain_ShouldNotReferenceForbiddenAssemblies()
    {
        // Arrange
        var forbiddenPrefixes = new[]
        {
            "PlanningApp.Application",
            "PlanningApp.Persistence",
            "PlanningApp.Infrastructure",
            "PlanningApp.WebApi",
            "MediatR",
            "FluentValidation",
            "Microsoft.EntityFrameworkCore",
            "Npgsql"
        };

        // Act
        var violations = FindForbiddenReferences(DomainAssembly, forbiddenPrefixes);

        // Assert
        violations.Should().BeEmpty(
            "Domain katmanı bu assembly'lere bağımlı olamaz; ihlal eden referanslar: {0}",
            string.Join(", ", violations));
    }

    [Fact]
    public void Application_ShouldNotReferenceForbiddenAssemblies()
    {
        // Arrange
        var forbiddenPrefixes = new[]
        {
            "PlanningApp.Persistence",
            "PlanningApp.Infrastructure",
            "PlanningApp.WebApi",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "Microsoft.AspNetCore"
        };

        // Act
        var violations = FindForbiddenReferences(ApplicationAssembly, forbiddenPrefixes);

        // Assert
        violations.Should().BeEmpty(
            "Application katmanı bu assembly'lere bağımlı olamaz; ihlal eden referanslar: {0}",
            string.Join(", ", violations));
    }

    // ---- Domain kuralları ----

    [Fact]
    public void Domain_ShouldNotHavePublicSetters()
    {
        // Arrange
        var properties = GetDomainTypes()
            .SelectMany(type => type.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly))
            .ToList();

        // Act: init erişimcisi (IsExternalInit modreq) kural dışıdır
        var violations = properties
            .Where(property => property.SetMethod is { IsPublic: true } setMethod && !IsInitOnly(setMethod))
            .Select(property => $"{property.DeclaringType!.FullName}.{property.Name}")
            .ToList();

        // Assert
        properties.Should().NotBeEmpty("Domain assembly'sinde property bulunmalı (test boş geçmemeli)");
        violations.Should().BeEmpty(
            "Domain'de public set yasaktır; ihlal eden property'ler: {0}",
            string.Join(", ", violations));
    }

    [Fact]
    public void Domain_NonExceptionClasses_ShouldNotHavePublicConstructors()
    {
        // Arrange
        var classes = GetDomainTypes()
            .Where(type => type.IsClass && type.IsPublic || type is { IsClass: true, IsNestedPublic: true })
            .Where(type => !type.IsEnum)
            .Where(type => !(type.IsAbstract && type.IsSealed)) // static sınıf
            .Where(type => !typeof(Exception).IsAssignableFrom(type))
            .ToList();

        // Act: nesneler yalnızca factory metodlarıyla oluşturulmalı
        var violations = classes
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(type => type.FullName!)
            .ToList();

        // Assert
        classes.Should().NotBeEmpty("Domain assembly'sinde sınıf bulunmalı (test boş geçmemeli)");
        violations.Should().BeEmpty(
            "Domain sınıfları (exception hariç) public constructor içeremez, factory kullanılmalı; ihlal eden tipler: {0}",
            string.Join(", ", violations));
    }

    // ---- Application kuralları ----

    [Fact]
    public void Application_QueryHandlers_ShouldNotDependOnUnitOfWork()
    {
        // Arrange
        var queryHandlers = ApplicationAssembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Namespace is not null && type.Namespace.Contains(".Queries."))
            .Where(ImplementsRequestHandler)
            .ToList();

        // Act: Query'ler durum değiştirmez, IUnitOfWork kullanamaz
        var violations = queryHandlers
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(constructor => constructor.GetParameters())
                .Any(parameter => parameter.ParameterType == typeof(IUnitOfWork)))
            .Select(type => type.FullName!)
            .ToList();

        // Assert
        queryHandlers.Should().NotBeEmpty("Application'da en az bir Query handler bulunmalı (test boş geçmemeli)");
        violations.Should().BeEmpty(
            "Query handler'ları IUnitOfWork'e bağımlı olamaz; ihlal eden tipler: {0}",
            string.Join(", ", violations));
    }

    // ---- Test Helpers ----

    private static List<string> FindForbiddenReferences(Assembly assembly, string[] forbiddenPrefixes)
        => assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

    // Derleyicinin ürettiği tipler (closure, async state machine vb.) hariç tüm Domain tipleri
    private static List<Type> GetDomainTypes()
        => DomainAssembly.GetTypes()
            .Where(type => !Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)))
            .ToList();

    private static bool IsInitOnly(MethodInfo setMethod)
        => setMethod.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static bool ImplementsRequestHandler(Type type)
        => type.GetInterfaces().Any(@interface =>
            @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
}
