using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EB.Sequence
{
	public class BooleanEvaluator
	{
	    /// <summary>
	    /// Evaluates the given expression and returns the result.
	    /// </summary>
	    public static bool Evaluate(string expression)
	    {
	        // Replace all the string elements like: false, true, and, or, with the equivalent bit
	        // representation of the expression, and also remove all the whitespace.
	        string bitExpression = expression.ToUpper().Replace("FALSE", "0").Replace("TRUE", "1");
	        bitExpression = bitExpression.Replace("AND", "&&").Replace("OR", "||").Replace(" ", "");
			
			bitExpression = bitExpression.Replace("!0","1");
			bitExpression = bitExpression.Replace("!1","0");
			
	        // Get a list of rules that can be used to decompose and evaluate an expression.
	        Dictionary<string, string> rules = GetRules();
	
	        // Continue looping through a set of rules that will decompose the operators until it is down
	        // to a single character (either a "0" or "1").
	        while (bitExpression.Length > 1)
	        {
	            // Loop through all of the rules and if one of the rules can be used to decompose part of
	            // the expression, apply the rule and set ruleApplied to true.
	            bool ruleApplied = false;
	            foreach (KeyValuePair<string, string> thisRule in rules)
	            {
	                // Check to see if this rule matches any part of the expression
	                if (bitExpression.Contains(thisRule.Key))
	                {
	                    bitExpression = bitExpression.Replace(thisRule.Key, thisRule.Value);
	                    ruleApplied = true;
	                }
					
					if (ruleApplied)
					{
						bitExpression = bitExpression.Replace("!(0)","(1)");
						bitExpression = bitExpression.Replace("!(1)","(0)");
					}
	            }
	
	            // If none of the rules could be used to evaluate the expression any further, and we are
	            // still not down to a single "0" or "1" ... break out of the loop because we will not
	            // be able to evaluate the expression.  An exception will be thrown for this case at the
	            // bottom of this method.  (Note: This could be because the expression contained unexpected
	            // characters, or if it was not well-formed.)
	            if (!ruleApplied)
	                break;
	        }
	
	        // If the expression evaluated to either "0" or "1", return the appropriate boolean variable.
	        if (bitExpression == "0")
	            return false;
	        else if (bitExpression == "1")
	            return true;
	
	        // If the expression didn't evaluate all the way down to 0 or 1, throw an exception
	        throw new Exception("Couldn't evaluate expression: " + expression);
	    }
	    /// <summary>
	    /// Returns a list of rules that can be used to decompose and evaluate an expression.
	    /// </summary>
	    private static Dictionary<string, string> GetRules()
	    {
	        Dictionary<string, string> rules = new Dictionary<string, string>();
	        rules.Add("(0)", "0");
	        rules.Add("(1)", "1");
	        rules.Add("0&&0", "0");
	        rules.Add("0&&1", "0");
	        rules.Add("1&&0", "0");
	        rules.Add("1&&1", "1");
	        rules.Add("0||0", "0");
	        rules.Add("0||1", "1");
	        rules.Add("1||0", "1");
	        rules.Add("1||1", "1");
	        return rules;
	    }
	}
}

