/*MESSAGE TO ANY FUTURE CODERS:
 PLEASE COMMENT YOUR WORK
 I can't stress how important this is especially with bomb types such as boss modules.
 If you don't it makes it realy hard for somone like me to find out how a module is working so I can learn how to make my own.
 Please comment your work.
 Short_c1rcuit*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using KModkit;


public class LogicStatement : MonoBehaviour
{
	public KMAudio Audio;
	public KMNeedyModule needy;

	//Used to tell if the bool is active, so buttons can't be pressed if it's not
	bool active;

	//Array to hold the true and false buttons
	public KMSelectable[] buttons;

	//Text on the display
	public TextMesh display;

	//Array to store the logic gate characters
	readonly char[] logicGates = new char[8] {'∧','∨','⊻','|','↓','↔','→','←'};

	//The two gates used in the statement
	char gate1, gate2;

	//This bool is used to store the end result of the statement
	string result;

	//The bool storing whether the bracket is on the left or the right
	bool bracketLeft;

	//The bool that stores the value of the bracket as a whole
	string bracket;

	//logging
	static int moduleIdCounter = 1;
	int moduleId;

	//Twitch help message
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Submit “True” with “!{0} T/True”. Submit “False” with “!{0} F/False”.";
#pragma warning disable 414

	//This part takes the command and sees if it says true or false then presses the correct button
	public KMSelectable[] ProcessTwitchCommand(string command)
	{
		command = command.ToLowerInvariant().Trim();
		if (command == "t" | command == "true")
		{
			return new[] { buttons[0] };
		}
		else if (command == "f" | command == "false")
		{
			return new[] { buttons[1] };
		}
		return null;
	}

	void Awake()
	{
		moduleId = moduleIdCounter++;

		//Sets up the needed methods for any needy
		GetComponent<KMNeedyModule>().OnNeedyActivation += OnNeedyActivation;
		GetComponent<KMNeedyModule>().OnNeedyDeactivation += OnNeedyDeactivation;
		GetComponent<KMNeedyModule>().OnTimerExpired += OnTimerExpired;

		//Sets up the methods for button presses
		foreach (KMSelectable button in buttons)
		{
			KMSelectable pressedButton = button;
			button.OnInteract += delegate () { ButtonPress(pressedButton); return false; };
		}
	}

	protected void OnNeedyActivation()
	{
		active = true;

		//Array to store the chars T or F on the statement
		string[] TorF = new string[3];

		//Sets the order the statement goes in
		bracketLeft = UnityEngine.Random.Range(0, 2) == 0;

		//Sets each value in the statement to either true or false and may add a NOT symbol infront
		foreach (string value in TorF)
		{
			if (UnityEngine.Random.Range(0, 2) == 0)
			{
				TorF[Array.IndexOf(TorF, value)] = "T";
			}
			else
			{
				TorF[Array.IndexOf(TorF, value)] = "F";
			}
		}

		//Randomly adds a NOT symbol to some of the values
		foreach (string value in TorF)
		{
			if (UnityEngine.Random.Range(0, 2) == 0)
			{
				TorF[Array.IndexOf(TorF, value)] = "¬" + value;
			}
		}

		//Generates two random logic gates
		gate1 = logicGates[UnityEngine.Random.Range(0, 8)];
		gate2 = logicGates[UnityEngine.Random.Range(0, 8)];

		TorF[0] = "¬F";
		TorF[1] = "¬F";
		TorF[2] = "F";

		gate1 = logicGates[6];
		gate2 = logicGates[2];

		//Displays the statement on the screen
		if (bracketLeft)
		{
			display.text = "(" + TorF[0] + gate1 + TorF[1] + ")" + gate2 + TorF[2];
		}
		else
		{
			display.text = TorF[2] + gate2 + "(" + TorF[0] + gate1 + TorF[1] + ")";
		}

		Debug.LogFormat("[Logic Statement #{0}] The statement is {1}.", moduleId, display.text);
		
		//Switches the value if there is a NOT symbol in front of it
		foreach (string value in TorF)
		{
			if (value[0] == '¬')
			{
				if (value[1] == 'T')
				{
					TorF[Array.IndexOf(TorF, value)] = "F";
				}
				else
				{
					TorF[Array.IndexOf(TorF, value)] = "T";
				}
			}
		}

		//Logs the statement with the NOT symbol values flipped
		if (bracketLeft)
		{
			Debug.LogFormat("[Logic Statement #{0}] Step 1: ({1} {2} {3}) {4} {5}.", moduleId, TorF[0], gate1, TorF[1], gate2, TorF[2]);
		}
		else
		{
			Debug.LogFormat("[Logic Statement #{0}] Step 1: {5} {4} ({1} {2} {3}).", moduleId, TorF[0], gate1, TorF[1], gate2, TorF[2]);
		}

		//Finds the value of the whole bracket
		bracket = SolveStatement(TorF[0], gate1, TorF[1]);

		//Logs the statement with the value of the whole bracket
		if (bracketLeft)
		{
			Debug.LogFormat("[Logic Statement #{0}] Step 2: {1} {2} {3}.", moduleId, bracket, gate2, TorF[2]);
		}
		else
		{
			Debug.LogFormat("[Logic Statement #{0}] Step 2: {3} {2} {1}.", moduleId, bracket, gate2, TorF[2]);
		}

		//Finds the value of the whole statement
		if (bracketLeft)
		{
			result = SolveStatement(bracket, gate2, TorF[2]);
		}
		else
		{
			result = SolveStatement(TorF[2], gate2, bracket);
		}

		//Logs the result
		Debug.LogFormat("[Logic Statement #{0}] Result: {1}.", moduleId, result);

	}

	protected void OnNeedyDeactivation()
	{
		//Resets the module
		active = false;
		display.text = "";
	}

	protected void OnTimerExpired()
	{
		//Resets the module
		active = false;
		display.text = "";
		//Logs the strike cause
		Debug.LogFormat("[Logic Statement #{0}] You ran out of time. Strike!.", moduleId);
		//Gives a strike for not going fast enough
		needy.HandleStrike();
	}

	void ButtonPress(KMSelectable button)
	{
		if (active)
		{
			//Makes the bomb move when you press it
			button.AddInteractionPunch();

			//Makes a sound when you press the button.
			GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

			//This XNOR gate returns true when the correct button is pressed (buttons[0] is the true button)
			if (!((button == buttons[0]) ^ (result == "T")))
			{
				//Stops the module
				needy.HandlePass();
				//Logs that they got it correct. As the button press is correct it will be the same as the result
				Debug.LogFormat("[Logic Statement #{0}] You pressed “{1}”. Correct!.", moduleId, result);
			}
			else
			{
				//Strikes the module
				needy.HandleStrike();
				needy.HandlePass();
				Debug.LogFormat("[Logic Statement #{0}] You pressed “{1}”. Incorrect. Strike!", moduleId, result);
			}
			//Resets the module
			OnNeedyDeactivation();
		}
	}

	string SolveStatement(string left, char gate, string right)
	{
		//AND gate
		if (gate == '∧')
		{
			if (left == "T" & right == "T")
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//OR gate
		else if (gate == '∨')
		{
			if (left == "T" | right == "T")
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//XOR gate
		else if (gate == '⊻')
		{
			if (left == "T" ^ right == "T")
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//NAND gate
		else if (gate == '|')
		{
			if (!(left == "T" & right == "T"))
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//NOR gate
		else if (gate == '↓')
		{
			if (!(left == "T" | right == "T"))
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//XNOR gate
		else if (gate == '↔')
		{
			if (!(left == "T" ^ right == "T"))
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//IMP LEFT gate
		else if (gate == '→')
		{
			if (!(left == "T" & right == "F"))
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
		//IMP RIGHT gate
		else
		{
			if (!(left == "F" & right == "T"))
			{
				return "T";
			}
			else
			{
				return "F";
			}
		}
	}
}