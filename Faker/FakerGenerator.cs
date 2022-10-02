using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Faker
{
    public class FakerGenerator
    {
        // public method for usege
        public T Create<T>()
        {
            Thread.Sleep(1);
            return (T)Create(typeof(T));
        }

        // private method for proceeding
        private object Create(Type t)
        {
            // Check whether there is cyclic dependency in the selected type or not
            if (!HasNotCyclicDependency(t))
                throw new Exception("ERROR! Cyclic Dependency was found.");

            // If we have a valueType variable -> create an instance with valueType
            // проверка ссылка это или нет
            if (t.IsValueType)
            {
                try // try catch block for properly unit testing logic
                {
                    if (!t.IsSecurityCritical && t.IsSecurityTransparent && t.IsSerializable)
                        return GenerateInstanceWithValueTypeVariable(t);
                    else
                        return GenerateInstanceWithAStructureVariable(t);
                }
                catch (Exception)
                {
                    return GenerateInstanceWithValueTypeVariable(t);
                }
            }
            else // Otherwise -> work with reference type
            {
                // If our reference type is a string - return a randomly generated string
                if (t.IsSerializable && t.IsSecurityTransparent && t.IsSealed && !t.IsSecurityCritical)
                    return GenerateInstanceWithAStringValue(t,10);

                // If our reference type is a generic type - create a generic variable
                if (t.IsGenericType)
                    return GenerateInstanceWithAGenericTypeVariable(t);

                return GenerateInstanceWithAClassTypeVariable(t);
            }
        }





        // Method wich return an instance with an instance based on type variable
        private object CreateInstance(Type t)
        {
            //устанавливает дефолт значения для примитивных типов и String
            if (t.IsValueType)
            {
                // Для типов-значений вызов конструктора по умолчанию даст default(T).
                return Activator.CreateInstance(t);
            }
            else
                // Для ссылочных типов значение по умолчанию всегда null.
                return null;
        }

        // Method wich will generate a value type variable
        private object GenerateInstanceWithValueTypeVariable(Type t)
        {
            Thread.Sleep(1);
            var instance = CreateInstance(t);

            var valueTypeGeneratorMethods = GetAllValueTypesGeneratorMethodsName();
           
            foreach (var item in valueTypeGeneratorMethods)
            {
                //NonPublic извлекает не публичные методы
                //Instance получает только методы экземпляра
                var temp = GetType().GetMethod(item, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, new object[] { });

                if (t != temp.GetType())
                    continue;
                else
                {
                    instance = temp;
                    return instance;
                }

            }
            throw new Exception("Error. Value type was not created.");
        }

        // Method wich will generate a string variable
        private object GenerateInstanceWithAStringValue(Type t, byte strLength)
        {
            Thread.Sleep(1);
            var instance = CreateInstance(t);

            try
            {
                instance = GenerateRandomString(strLength);
                return instance;
            }
            catch (Exception)
            {
                throw new Exception("Error. String type was not created.");
            }
            
        }
        private string GenerateRandomString(int length)
        {
            Random random = new Random();
            //StringBuilder Предоставляет изменяемую строку символов
            //так как при добавлении новых подстрок String постоянно выделяет нужное количество памяти
            //то приходится заново перекопировать,что неэффективно . StringBuilder выделяет память с небольшим запасом,что делает его более гибким
            var sb = new StringBuilder(string.Empty);

            for (int i = 0; i < length; i++)
            {
                sb.Append(Convert.ToChar(random.Next(97, 123)));
            }

            return sb.ToString();
        }

        // Method wich will generate a generic variable
        private object GenerateInstanceWithAGenericTypeVariable(Type t)
        {
            Thread.Sleep(1);
            //Activator Содержит методы, позволяющие локально или удаленно создавать типы объектов или получать ссылки на существующие удаленные объекты.
            //Метод CreateInstance создает экземпляр типа, определенного в сборке, путем вызова конструктора, который лучше всего соответствует указанным аргументам.
            var instance = (IList)Activator.CreateInstance(t);

            var genericTypeInsideVariable = t.GenericTypeArguments.FirstOrDefault();

            instance = GenerateRandomList(genericTypeInsideVariable, instance, 5);

            return instance;
        }

        // Method wich will generate a class variable
        private object GenerateInstanceWithAClassTypeVariable(Type t)
        {
            var maxParamConstructor = FindAConstructorWithMaxParametersNumber(t);
            var parameters = GenerateParamsForAClassTypeVariables(t);

            // Generating an object with a random data 
            var randomlyGeneratedObject = maxParamConstructor.Invoke(parameters);

            var propertyInfos = randomlyGeneratedObject.GetType().GetProperties().Where(p => !p.SetMethod.IsPrivate).ToList();
            var publicFields = randomlyGeneratedObject.GetType().GetFields().Where(p => !p.IsPrivate).ToList();

            for (int i = 0; i < propertyInfos.Count; i++)
            {
                var generatedObjectType = randomlyGeneratedObject.GetType();
                var objectPropertyTakenByName = generatedObjectType.GetProperty(propertyInfos[i].Name);

                var objectPropertyType = objectPropertyTakenByName.GetValue(randomlyGeneratedObject).GetType();
                var generatedProperty = this.Create(objectPropertyType);

                objectPropertyTakenByName.SetValue(randomlyGeneratedObject,
                generatedProperty, null);
            }

            for (int i = 0; i < publicFields.Count; i++)
            {
                var generatedObjectType = randomlyGeneratedObject.GetType();
                var objectFieldTakenByName = generatedObjectType.GetField(publicFields[i].Name);

                var objectFieldType = objectFieldTakenByName.GetValue(randomlyGeneratedObject).GetType();
                var generatedProperty = this.Create(objectFieldType);

                objectFieldTakenByName.SetValue(randomlyGeneratedObject,
                generatedProperty);
            }

            // Return created object
            return randomlyGeneratedObject;
        }
        private object[] GenerateParamsForAClassTypeVariables(Type t)
        {
            ConstructorInfo maxParamConstructor = FindAConstructorWithMaxParametersNumber(t);

            // Getting its parameters information
            ParameterInfo[] parametersInfo = maxParamConstructor.GetParameters();

            // Generating an array wich will have random generated parameters to create a new object Type t
            object[] parameters = new object[parametersInfo.Length];

            // Filling array of object with a random data using the recursion
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = this.Create(parametersInfo[i].ParameterType);
            }

            return parameters;
        }
        private ConstructorInfo FindAConstructorWithMaxParametersNumber(Type t)
        {
            // Getting all class constructors
            var constructorInfoObjects = t.GetConstructors();

            // Looking for constructor with the biggest number of parameters
            int ctorParametersLength = 0;
            byte ctorIndex = 0;
            byte counter = 0;
            foreach (var constructor in constructorInfoObjects)
            {
                if (constructor.GetParameters().Length > ctorParametersLength)
                {
                    ctorIndex = counter;
                    ctorParametersLength = constructor.GetParameters().Length;
                }
                counter++;
            }

            // Getting constructor with the biggest number of parameters
            var maxParamConstructor = constructorInfoObjects.Where(c => c.GetParameters().Length == ctorParametersLength).First();

            return maxParamConstructor;
        }

        // Method wich will generate a struc variable
        private object GenerateInstanceWithAStructureVariable(Type t)
        {
            // If we deal with a structure -> we can create it the same way as a class type variable
            return GenerateInstanceWithAClassTypeVariable(t);
        }

        // Methods wich generate random value type variables
        private int GenerateRandomIntegerNumber()
        {
            Random random = new Random();
            return random.Next(int.MinValue, int.MaxValue);
        }
        private double GenerateRandomDoubleNumber()
        {
            Random random = new Random();
            return random.NextDouble() + random.Next(0, int.MaxValue);
        }
        private bool GenerateRandomBoolValue()
        {
            Random random = new Random();
            int intBool = random.Next(0,2);

            if (intBool == 1)
                return true;
            else
                return false;
        }
        private long GenerateRandomLongNumber()
        {
            Random random = new Random();
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
        private float GenerateRandomFloatNumber()
        {
            Random random = new Random();

            var array = new byte[4];
            random.NextBytes(array);

            return BitConverter.ToSingle(array, 0);
        }
        private byte GenerateRandomByteNumber()
        {
            Random random = new Random();
            return (byte)random.Next(0, 256);
        }
        private char GenerateRandomCharValue()
        {
            Random random = new Random();
            char result = Convert.ToChar(random.Next(97, 123));

            bool append = GenerateRandomBoolValue();

            if (append)
                return Convert.ToChar(result.ToString().ToUpper());
            else
                return result;
        }
        private short GenerateRandomShortNumber()
        {
            Random random = new Random();
            return (short)random.Next(short.MinValue, short.MaxValue);
        }
        private decimal GenerateRandomDecimalNumber()
        {
            Random random = new Random();
            return (decimal)GenerateRandomDoubleNumber();
        }

        // Methods wich will generate random data with a generic variables

        private IList GenerateRandomList(Type t, IList emptyList, int countOfTheVariablesInside)
        {
            // Create a specific amount of variables
            for (int i = 0; i < countOfTheVariablesInside; i++)
            {
                // Searching for a constructor with maxed number of parameters
                object variable;
                var sortedCtors = t.GetConstructors().ToList();

                sortedCtors.Sort((x, y) => x.GetParameters().Length.CompareTo(y.GetParameters().Length));
                var ctor = sortedCtors.FirstOrDefault();
                
                // If there is no constructor with parameters -> create default instance
                if (ctor == null || ctor.GetParameters().Length == 0)
                {
                    variable = Activator.CreateInstance(t);
                }
                else // Otherwise -> create instance using the specific object parameters
                {
                    variable = Activator.CreateInstance(t, GenerateParamsForAClassTypeVariables(t));
                }

                // Convert an object type to the needed type
                var createdItem = Convert.ChangeType(this.Create(t), t);

                emptyList.Add(createdItem);
            }    
            return emptyList;
        }

        // Making a list of all methods wich output is a value type variable
        private List<string> GetAllValueTypesGeneratorMethodsName()
        {
            List<string> result = new List<string>();

            result.Add(nameof(GenerateRandomDoubleNumber));
            result.Add(nameof(GenerateRandomDecimalNumber));
            result.Add(nameof(GenerateRandomFloatNumber));
            result.Add(nameof(GenerateRandomLongNumber));
            result.Add(nameof(GenerateRandomIntegerNumber));
            result.Add(nameof(GenerateRandomShortNumber));
            result.Add(nameof(GenerateRandomCharValue));
            result.Add(nameof(GenerateRandomByteNumber));
            result.Add(nameof(GenerateRandomBoolValue));

            return result;
        }

        // Main method wich is looking for a cyclic dependency
        private bool HasNotCyclicDependency(Type t)
        {
            // Getting main type properties 
            var objectProperties = t.GetProperties().ToList();

            foreach (var property in objectProperties)
            {
                if (WrongInsideDependency(t, property.PropertyType))
                    return false;
            }
            return true;
        }

        // Supply method wich is looking for a cyclic dependency using the recursion
        private bool WrongInsideDependency(Type baseType, Type currentType)
        {
            // Getting current type properties 
            var currObjectProperties = currentType.GetProperties().ToList();

            foreach (var property in currObjectProperties)
            {
                // If we have the same property wich is equals to the base type -> cyclic dependency was found
                if (property.PropertyType == baseType)
                    return true;
                else // If not - start another recursion search
                    WrongInsideDependency(baseType, property.PropertyType);
            }
            return false;
        }
    }
}
