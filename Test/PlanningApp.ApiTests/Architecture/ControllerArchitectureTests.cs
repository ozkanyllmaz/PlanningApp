using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using PlanningApp.WebApi.Controllers;

namespace PlanningApp.ApiTests.Architecture;

// Controller kurallarını reflection ile doğrular (bkz. api_test_rules.md → Mimari testler)
public class ControllerArchitectureTests
{
    private const string RoutePrefix = "api/v1/";

    // Yeni bir anonim uç eklemek bu listeyi BİLİNÇLİ olarak güncellemeyi gerektirir
    private static readonly string[] ExpectedAnonymousActions =
    [
        "AuthController.Login",
        "AuthController.Logout",
        "AuthController.Refresh",
        "AuthController.Register"
    ];

    private static readonly Type[] ControllerTypes = typeof(AuthController).Assembly
        .GetTypes()
        .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
        .ToArray();

    // ---- Kural 1: yalnızca ISender ----

    [Fact]
    public void Controllers_ShouldOnlyDependOnISender()
    {
        // Arrange & Act
        var violations = ControllerTypes
            .Where(type =>
            {
                var constructors = type.GetConstructors();
                return constructors.Length != 1
                    || constructors[0].GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual([typeof(ISender)]) is false;
            })
            .Select(type => type.Name)
            .ToList();

        // Assert
        ControllerTypes.Should().NotBeEmpty();
        violations.Should().BeEmpty("controller constructor'ı yalnızca ISender almalıdır");
    }

    // ---- Kural 2: tüm yollar api/v1/ ile başlar ----

    [Fact]
    public void AllRoutes_ShouldStartWithApiV1()
    {
        // Arrange & Act
        var violations = new List<string>();
        foreach (var controller in ControllerTypes)
        {
            var controllerTemplate = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;

            foreach (var action in GetActions(controller))
            {
                var actionTemplate = action.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault()?.Template;
                var fullPath = actionTemplate is not null && actionTemplate.StartsWith('/')
                    ? actionTemplate.TrimStart('/')
                    : $"{controllerTemplate}/{actionTemplate}".Trim('/');

                if (!fullPath.StartsWith(RoutePrefix, StringComparison.Ordinal))
                    violations.Add($"{controller.Name}.{action.Name}: {fullPath}");
            }
        }

        // Assert
        violations.Should().BeEmpty();
    }

    // ---- Kural 3: anonim uçlar beklenen kümeyle birebir aynı ----

    [Fact]
    public void AnonymousActions_ShouldMatchExpectedSetExactly()
    {
        // Arrange & Act
        var anonymousActions = ControllerTypes
            .SelectMany(controller => GetActions(controller)
                .Where(action => action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                                 || controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
                .Select(action => $"{controller.Name}.{action.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Assert
        anonymousActions.Should().Equal(ExpectedAnonymousActions);
    }

    // ---- Test Helpers ----

    private static IEnumerable<MethodInfo> GetActions(Type controller)
        => controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(HttpMethodAttribute), inherit: true));
}
