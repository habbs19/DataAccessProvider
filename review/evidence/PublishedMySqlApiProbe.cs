using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using DataAccessProvider.MySql;
var type=typeof(DbParameterExtensions);
var api=type.GetMethods(BindingFlags.Public|BindingFlags.Static).Where(m=>m.Name=="AddJSONParams").Select(m=>new {
 signature=m.ToString(),parameters=m.GetParameters().Select(p=>new {p.Name,p.IsOptional,defaultValue=p.DefaultValue is DBNull ? "<DBNull>" : p.DefaultValue})
});
var assembly=type.Assembly.Location;
Console.WriteLine(JsonSerializer.Serialize(new {assembly,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly))),api},new JsonSerializerOptions {WriteIndented=true}));
