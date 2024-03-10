using NSwag.CodeGeneration.OperationNameGenerators;
using NSwag.CodeGeneration.TypeScript;

var document = await NSwag.OpenApiDocument.FromUrlAsync("http://localhost:4900/swagger/v1/swagger.json");

var settings = new TypeScriptClientGeneratorSettings
{
    ClassName = "{controller}Client",
    Template = TypeScriptTemplate.Angular,
    HttpClass = HttpClass.HttpClient,
    RxJsVersion = 7.8M,
    InjectionTokenType = InjectionTokenType.InjectionToken,
    UseSingletonProvider = true,
    TypeScriptGeneratorSettings =
    {
        TypeStyle = NJsonSchema.CodeGeneration.TypeScript.TypeScriptTypeStyle.Interface,
        DateTimeType = NJsonSchema.CodeGeneration.TypeScript.TypeScriptDateTimeType.String,
        NullValue = NJsonSchema.CodeGeneration.TypeScript.TypeScriptNullValue.Undefined,
        MarkOptionalProperties = true, //f
        GenerateConstructorInterface = true,
        GenerateCloneMethod = false, //t
        GenerateDefaultValues = false, //t
        TypeScriptVersion = 5.2M,
    }
};

var generator = new TypeScriptClientGenerator(document, settings);
var code = generator.GenerateFile();
var rootDirectory = Directory.GetCurrentDirectory();
#if (DEBUG)
    //When debugging, the current directory is `nswag\bin\Debug\net8.0`, but I need it to be just `nswag`
    rootDirectory = new DirectoryInfo(rootDirectory).Parent!.Parent!.Parent!.Parent!.FullName;
#else
    //but in release mode, I'm gonna assume that this is run from the same directory as the nswag.csproj file
    rootDirectory = new DirectoryInfo(rootDirectory).Parent!.FullName;
#endif
var filepath = Path.Combine(rootDirectory, "UI", "nswag-generated.ts");
File.WriteAllText(filepath, code);
Console.WriteLine("Successfully Generated!");
#if (DEBUG)
    Thread.Sleep(1000);
#endif