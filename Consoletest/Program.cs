using ConsoletestA;

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;

namespace Consoletest
{
    internal class Program
    {

        private static bool a1 = false;
        private static string a2 = a2 ?? throw new NullReferenceException(); 

        static Program()
        {
            Console.WriteLine("static");
        }

        static async Task Main(string[] args)
        {   
            //a1 = true;
            Console.WriteLine("test");
            await Task.Delay(1000);           
        }
        static string NameOfFullName<T>(LambdaExpression expr,bool isFullName = true)
        {
            static string GetFullNameMethod(MethodCallExpression method)
            {
                Type declaringType = method.Method.DeclaringType ?? throw new NullReferenceException(nameof(Type.DeclaringType));
                string fullName = $"{method.Method.ReturnType} {declaringType.Namespace}.{method.Method.DeclaringType.Name}.{method.Method.Name}(";
                foreach (var parameter in method.Method.GetParameters())
                {
                    fullName += $"{parameter.ParameterType} {parameter.Name}, ";
                }
                fullName = fullName.TrimEnd(',', ' ') + ")";
                return fullName ;
            }
            static string GetFullNameMember(MemberExpression member)
            {
                string memberName = member.Member.Name;
                PropertyInfo propInfo = member.Member as PropertyInfo ?? throw new ArgumentException("Expression must be a property expression.", nameof(member));

                string accessModifiers = "";
                MethodInfo getMethod = propInfo.GetGetMethod(nonPublic: true) ?? throw new NullReferenceException(nameof(PropertyInfo.GetMethod));
                MethodInfo setMethod = propInfo.GetSetMethod(nonPublic: true) ?? throw new NullReferenceException(nameof(PropertyInfo.GetMethod));

                if (getMethod is not null)
                {
                    accessModifiers += getMethod.IsPublic ? "public " : "internal ";
                    accessModifiers += getMethod.IsStatic ? "static " : "";
                    accessModifiers += "get; ";
                }

                if (setMethod is not null)
                {
                    accessModifiers += setMethod.IsPublic ? "public " : "internal ";
                    accessModifiers += setMethod.IsStatic ? "static " : "";
                    accessModifiers += "set; ";
                }

                Type type = propInfo.DeclaringType ?? throw new NullReferenceException(nameof(Type));
                string namespaceName = type.Namespace ?? throw new NullReferenceException(nameof(Type.Namespace));
                string typeName = type.FullName ?? throw new NullReferenceException(nameof(Type.FullName));

                return $"{propInfo.PropertyType.FullName} {typeName.Replace("+", ".")}.{memberName} {{ {accessModifiers}}}";
            }

            return expr.Body switch
            {
                MemberExpression memberExpr => isFullName is true ? GetFullNameMember(memberExpr) : memberExpr.Member.Name,
                MethodCallExpression methodCallExpr => isFullName is true ? GetFullNameMethod(methodCallExpr) : methodCallExpr.Method.Name,
                _ => throw new ArgumentException("Expression must be either a member access or method call expression."),
            };
        }
        public static class Helper
        {
            static string NameOfPropertyOrMethod<T>(LambdaExpression expr, bool isFullName = true)
            {
                static string GetFullNameMethod(MethodCallExpression method)
                {
                    Type declaringType = method.Method.DeclaringType ?? throw new NullReferenceException(nameof(Type.DeclaringType));
                    string fullName = $"{method.Method.ReturnType} {declaringType.Namespace}.{method.Method.DeclaringType.Name}.{method.Method.Name}(";
                    foreach (var parameter in method.Method.GetParameters())
                    {
                        fullName += $"{parameter.ParameterType} {parameter.Name}, ";
                    }
                    fullName = fullName.TrimEnd(',', ' ') + ")";
                    return fullName;
                }
                static string GetFullNameMember(MemberExpression member)
                {
                    string memberName = member.Member.Name;
                    PropertyInfo propInfo = member.Member as PropertyInfo ?? throw new ArgumentException("Expression must be a property expression.", nameof(member));

                    string accessModifiers = "";
                    MethodInfo getMethod = propInfo.GetGetMethod(nonPublic: true) ?? throw new NullReferenceException(nameof(PropertyInfo.GetMethod));
                    MethodInfo setMethod = propInfo.GetSetMethod(nonPublic: true) ?? throw new NullReferenceException(nameof(PropertyInfo.GetMethod));

                    if (getMethod is not null)
                    {
                        accessModifiers += getMethod.IsPublic ? "public " : "internal ";
                        accessModifiers += getMethod.IsStatic ? "static " : "";
                        accessModifiers += "get; ";
                    }

                    if (setMethod is not null)
                    {
                        accessModifiers += setMethod.IsPublic ? "public " : "internal ";
                        accessModifiers += setMethod.IsStatic ? "static " : "";
                        accessModifiers += "set; ";
                    }

                    Type type = propInfo.DeclaringType ?? throw new NullReferenceException(nameof(Type));
                    string namespaceName = type.Namespace ?? throw new NullReferenceException(nameof(Type.Namespace));
                    string typeName = type.FullName ?? throw new NullReferenceException(nameof(Type.FullName));

                    return $"{propInfo.PropertyType.FullName} {typeName.Replace("+", ".")}.{memberName} {{ {accessModifiers}}}";
                }

                return expr.Body switch
                {
                    MemberExpression memberExpr => isFullName is true ? GetFullNameMember(memberExpr) : memberExpr.Member.Name,
                    MethodCallExpression methodCallExpr => isFullName is true ? GetFullNameMethod(methodCallExpr) : methodCallExpr.Method.Name,
                    _ => throw new ArgumentException("Expression must be either a member access or method call expression."),
                };
            }

        }

        public static class StaticClass
        {
            public static event EventHandler StaticEvent;
        }

   
    }

    internal class A123
    {
        private string a2;
        private A123( string bbb) 
        {
            a2 = bbb;

        }
        internal static async Task<A123> Create()
        {
            return new A123(await getStr());
        }
        private static Task<string> getStr()
        {
            return Task.FromResult("Test0000");
        }
    }
}
namespace ConsoletestA
{
    public class A1
    {
        public class A2
        {
            public void Test(int a, int ab)
            {

            }
            public object Test2(int a, int ab)
            {
                return new object();
            }
            public decimal Myprop { get; set; }
        }
    }
}
