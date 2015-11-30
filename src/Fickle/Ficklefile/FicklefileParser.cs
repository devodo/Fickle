﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Fickle.Model;
using Platform.Reflection;

namespace Fickle.Ficklefile
{
	public class FicklefileParser
	{
		private readonly FicklefileTokenizer tokenizer;
		private ServiceModelInfo serviceModelInfo;
		private readonly List<ServiceEnum> enums = new List<ServiceEnum>();
		private readonly List<ServiceClass> classes = new List<ServiceClass>();
		private readonly List<ServiceGateway> gateways = new List<ServiceGateway>();
	
		public FicklefileParser(TextReader reader)
		{
			this.tokenizer = new FicklefileTokenizer(reader);

			this.tokenizer.ReadNextToken();
		}

		public static ServiceModel Parse(string s)
		{
			return Parse(new StringReader(s));
		}

		public static ServiceModel Parse(TextReader reader)
		{
			var parser = new FicklefileParser(reader);

			return parser.Parse();
		}

		protected virtual void ProcessTopLevel()
		{
			if (this.tokenizer.CurrentToken == FicklefileToken.Keyword)
			{
				switch (this.tokenizer.CurrentKeyword)
				{
				case FicklefileKeyword.Info:
					this.serviceModelInfo = this.ProcessInfo();
					break;
				case FicklefileKeyword.Class:
					this.classes.Add(this.ProcessClass());
					break;
				case FicklefileKeyword.Enum:
					this.enums.Add(this.ProcessEnum());
					break;
				case FicklefileKeyword.Gateway:
					this.gateways.Add(this.ProcessGateway());
					break;
				}
			}
			else
			{
				throw new UnexpectedFicklefileTokenException(this.tokenizer.CurrentToken, this.tokenizer.CurrentValue, FicklefileToken.Keyword);
			}
		}

		protected virtual void Expect(params FicklefileToken[] tokens)
		{
			if (!tokens.Contains(this.tokenizer.CurrentToken))
			{
				throw new UnexpectedFicklefileTokenException(this.tokenizer.CurrentToken, this.tokenizer.CurrentValue, tokens);
			}
		}

		protected virtual ServiceEnumValue ProcessEnumValue()
		{
			var retval = new ServiceEnumValue
			{
				Name = this.tokenizer.CurrentIdentifier
			};

			this.ReadNextToken();

			if (this.tokenizer.CurrentToken == FicklefileToken.Colon)
			{
				this.ReadNextToken();

				this.Expect(FicklefileToken.Integer);

				retval.Value = (int)this.tokenizer.CurrentInteger;

				this.ReadNextToken();
			}

			return retval;
		}
			
		protected virtual ServiceEnum ProcessEnum()
		{
			this.ReadNextToken();

			this.Expect(FicklefileToken.Identifier);

			var retval = new ServiceEnum
			{
				Name = this.tokenizer.CurrentIdentifier,
				Values = new List<ServiceEnumValue>()
			};

			this.ReadNextToken();
			this.Expect(FicklefileToken.Indent);
			this.ReadNextToken();

			while (true)
			{
				if (this.tokenizer.CurrentToken != FicklefileToken.Identifier)
				{
					break;
				}

				var enumValue = this.ProcessEnumValue();

				retval.Values.Add(enumValue);
			}

			this.Expect(FicklefileToken.Dedent);
			this.ReadNextToken();

			return retval;
		}

		protected virtual string ParseTypeName()
		{
			var builder = new StringBuilder();

			if (this.tokenizer.CurrentToken == FicklefileToken.OpenBracket)
			{
				builder.Append('[');

				this.ReadNextToken();
				builder.Append(this.ParseTypeName());

				this.Expect(FicklefileToken.CloseBracket);
				builder.Append(']');
				this.ReadNextToken();

				if (this.tokenizer.CurrentToken == FicklefileToken.QuestionMark)
				{
					builder.Append('?');
					this.ReadNextToken();
				}
			}
			else
			{
				this.Expect(FicklefileToken.Identifier);

				builder.Append(this.tokenizer.CurrentIdentifier);
				this.ReadNextToken();

				if (this.tokenizer.CurrentToken == FicklefileToken.QuestionMark)
				{
					builder.Append('?');
					this.ReadNextToken();
				}

				if (this.tokenizer.CurrentToken == FicklefileToken.OpenBracket)
				{	
					this.ReadNextToken();
					this.Expect(FicklefileToken.CloseBracket);
					this.ReadNextToken();
					builder.Append("[]");
				}
			}

			return builder.ToString();
		}

		protected virtual ServiceProperty ProcessProperty()
		{
			var retval = new ServiceProperty
			{
				Name = this.tokenizer.CurrentIdentifier
			};

			this.ReadNextToken();

			this.Expect(FicklefileToken.Colon);

			this.ReadNextToken();

			retval.TypeName = this.ParseTypeName();
			
			return retval;
		}


		protected virtual ServiceModelInfo ProcessInfo()
		{
			this.ReadNextToken();

			var retval = new ServiceModelInfo();

			if (this.tokenizer.CurrentToken == FicklefileToken.Indent)
			{
				this.ReadNextToken();

				while (true)
				{
					if (this.tokenizer.CurrentToken == FicklefileToken.Annotation)
					{
						var annotation = this.ProcessAnnotation();

						var property = retval.GetType().GetProperty(annotation.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

						if (property == null)
						{
							throw new FicklefileParserException($"Unexpected annotation: {annotation.Key}={annotation.Value}");
						}

						property.SetValue(retval, Convert.ChangeType(annotation.Value, property.PropertyType));
					}
					else
					{
						break;
					}
				}

				this.Expect(FicklefileToken.Dedent);
				this.ReadNextToken();
			}

			return retval;
		}

		protected virtual ServiceClass ProcessClass()
		{
			this.ReadNextToken();

			this.Expect(FicklefileToken.Identifier);

			var retval = new ServiceClass(this.tokenizer.CurrentIdentifier, null, new List<ServiceProperty>());

			this.ReadNextToken();

			if (this.tokenizer.CurrentToken == FicklefileToken.Indent)
			{
				this.ReadNextToken();

				while (true)
				{
					if (this.tokenizer.CurrentToken == FicklefileToken.Identifier)
					{
						var property = this.ProcessProperty();

						retval.Properties.Add(property);
					}
					else if (this.tokenizer.CurrentToken == FicklefileToken.Annotation)
					{
						var annotation = this.ProcessAnnotation();

						switch (annotation.Key)
						{
							case "extends":
								retval.BaseTypeName = annotation.Value;
								break;
							default:
								this.SetAnnotation(retval, annotation);
								break;
						}
					}
					else
					{
						break;
					}
				}

				this.Expect(FicklefileToken.Dedent);
				this.ReadNextToken();
			}

			return retval;
		}
		
		private void ReadNextToken()
		{
			this.tokenizer.ReadNextToken();
		}

		protected virtual ServiceParameter ProcessParameter()
		{
			var retval = new ServiceParameter()
			{
				Name = this.tokenizer.CurrentIdentifier
			};

			this.ReadNextToken();
			this.Expect(FicklefileToken.Colon);

			this.ReadNextToken();
			this.Expect(FicklefileToken.Identifier);

			retval.TypeName = this.tokenizer.CurrentIdentifier;

			this.ReadNextToken();

			if (this.tokenizer.CurrentToken == FicklefileToken.QuestionMark)
			{
				retval.TypeName += "?";

				this.ReadNextToken();
			}
			
			if (this.tokenizer.CurrentToken == FicklefileToken.OpenBracket)
			{
				this.ReadNextToken();
				this.Expect(FicklefileToken.CloseBracket);
				this.ReadNextToken();
				retval.TypeName += "[]";
			}

			return retval;
		}

		protected virtual ServiceMethod ProcessMethod()
		{
			var retval = new ServiceMethod
			{
				Name = this.tokenizer.CurrentIdentifier
			};

			this.ReadNextToken();

			if (this.tokenizer.CurrentToken == FicklefileToken.OpenParen)
			{
				this.ReadNextToken();

				var parameters = new List<ServiceParameter>();

				while (this.tokenizer.CurrentToken != FicklefileToken.CloseParen && this.tokenizer.CurrentToken != FicklefileToken.EndOfFile)
				{
					parameters.Add(this.ProcessParameter());
				}

				retval.Parameters = parameters;

				this.ReadNextToken();
			}

			if (this.tokenizer.CurrentToken == FicklefileToken.Indent)
			{
				this.ReadNextToken();

				while (this.tokenizer.CurrentToken != FicklefileToken.Dedent
					&& this.tokenizer.CurrentToken != FicklefileToken.EndOfFile)
				{
					if (this.tokenizer.CurrentToken == FicklefileToken.Annotation)
					{
						var annotation = this.ProcessAnnotation();

						this.SetAnnotation(retval, annotation);

						if (annotation.Key == "content")
						{
							var contentParameterName = annotation.Value.Trim();

							var serviceParameter = retval.Parameters.FirstOrDefault(c => c.Name == contentParameterName);

							retval.ContentServiceParameter = serviceParameter;
						}
					}
					else
					{
						throw new UnexpectedFicklefileTokenException(this.tokenizer.CurrentToken, null, FicklefileToken.Annotation);
					}
				}

				this.Expect(FicklefileToken.Dedent);

				this.ReadNextToken();
			}

			return retval;
		}

		private KeyValuePair<string, string> ProcessAnnotation()
		{
			var annotationName = this.tokenizer.CurrentString;

			this.tokenizer.ReadStringToEnd();

			var annotationValue = this.tokenizer.CurrentString.Trim();

			this.ReadNextToken();

			return new KeyValuePair<string, string>(annotationName, annotationValue);
		}

		private bool SetAnnotation(object target, KeyValuePair<string, string> annotation)
		{
			var property = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(c => c.Name.Equals(annotation.Key, StringComparison.InvariantCultureIgnoreCase));

			if (property != null)
			{
				if (property.GetFirstCustomAttribute<ServiceAnnotationAttribute>(true) == null)
				{
					return false;
				}

				if (property.PropertyType.GetUnwrappedNullableType() == typeof(bool))
				{
					var s = annotation.Value.Trim();

					property.SetValue(target, s == "yes" || s == "true" || s == "1", null);
				}
				else
				{
					property.SetValue(target, Convert.ChangeType(annotation.Value.Trim(), property.PropertyType), null);
				}

				return true;
			}

			return false;
		}

		protected virtual ServiceGateway ProcessGateway()
		{
			this.ReadNextToken();

			this.Expect(FicklefileToken.Identifier);

			var retval = new ServiceGateway
			{
				Name = this.tokenizer.CurrentIdentifier,
				Methods = new List<ServiceMethod>()
			};

			this.ReadNextToken();
			this.Expect(FicklefileToken.Indent);
			this.ReadNextToken();

			while (true)
			{
				if (this.tokenizer.CurrentToken == FicklefileToken.Identifier)
				{
					var method = this.ProcessMethod();

					retval.Methods.Add(method);
				}
				else if (this.tokenizer.CurrentToken == FicklefileToken.Annotation)
				{
					var annotation = this.ProcessAnnotation();

					this.SetAnnotation(retval, annotation);
				}
				else
				{
					break;
				}
			}

			this.Expect(FicklefileToken.Dedent);
			this.ReadNextToken();

			return retval;
		}

		protected virtual ServiceModel Parse()
		{
			while (this.tokenizer.CurrentToken != FicklefileToken.EndOfFile)
			{
				this.ProcessTopLevel();
			}

			return new ServiceModel(this.serviceModelInfo, this.enums, this.classes, this.gateways);
		}
	}
}
