using System;
using System.Collections.Generic;
using System.Numerics;

namespace TombLib.LevelData.VisualScripting
{
    public struct TriggerNodeArgument
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    // Input variable represents a slot that can accept data from another node's output
    public class InputVariable
    {
        public string Name { get; set; } = string.Empty;
        public Guid LinkedOutputNodeId { get; set; } = Guid.Empty;  // ID of the node providing the output
        public string LinkedOutputName { get; set; } = string.Empty;      // Name of the specific output variable
        public bool IsLinked => LinkedOutputNodeId != Guid.Empty && !string.IsNullOrEmpty(LinkedOutputName);
    }

    // Output variable represents a slot that can provide data to another node's input
    public class OutputVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;  // Data type (e.g., "String", "Numerical", "Boolean")
    }

    // Dynamic arguments that can be set by user or linked from another node
    public class DynamicData
    {
        public Dictionary<string, string> UserDefinedArguments { get; } = new Dictionary<string, string>();
    }

    // Every node in visual trigger has this set of parameters. Name and color are
    // merely UI properties, while Previous/Next and ScreenPosition determines the
    // order of compilation. Every node may have or may have no any previous or
    // next nodes. If node or group of nodes is orphaned, it's treated as a whole
    // code block. If visual trigger consists of several orphaned nodes or node
    // groups, they will be compiled into single function body in order determined
    // from their screen position: top to bottom.

    // Function determines an internal lua function which is called to perform certain
    // action based on node setup. These functions are not meant to be directly called
    // from level script, but they use similar notation. Such functions may have
    // several arguments, which are boxed to string values from UI controls of a node.

    public abstract class TriggerNode : ICloneable
    {
        public static int DefaultSize = 400;

        // Unique identifier for each node instance to support unambiguous linking
        public Guid Id { get; protected set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;
        public int Size { get; set; } = DefaultSize;
        public Vector3 Color { get; set; } = Vector3.Zero;
        public bool Locked { get; set; } = false;

        public string Function { get; set; } = string.Empty;
        public List<TriggerNodeArgument> Arguments { get; private set; } = new List<TriggerNodeArgument>();

        // New properties for enhanced node linking functionality
        public List<InputVariable> Inputs { get; private set; } = new List<InputVariable>();
        public List<OutputVariable> Outputs { get; private set; } = new List<OutputVariable>();
        public DynamicData DynamicArguments { get; private set; } = new DynamicData();
        public List<string> AllowedEventModes { get; private set; } = new List<string>();

        public TriggerNode Previous { get; set; }
        public TriggerNode Next { get; set; }


        public Vector2 ScreenPosition 
        { 
            get {  return _screenPosition; }
            set
            {
                // Don't update value if it went out of bounds (happens on layout update).
                if (value.X < 0 || value.X > 256 || value.Y < 0 || value.Y > 256)
                    return;

                _screenPosition = value;
            }
        }
        private Vector2 _screenPosition = Vector2.Zero;

        public virtual TriggerNode Clone()
        {
            var node = (TriggerNode)MemberwiseClone();
            // Generate new ID for cloned node to ensure uniqueness
            node.Id = Guid.NewGuid();
            node.Arguments = new List<TriggerNodeArgument>(Arguments);
            
            // Clone new properties
            node.Inputs = new List<InputVariable>();
            foreach (var input in Inputs)
            {
                node.Inputs.Add(new InputVariable
                {
                    Name = input.Name,
                    LinkedOutputNodeId = input.LinkedOutputNodeId,
                    LinkedOutputName = input.LinkedOutputName
                });
            }
            
            node.Outputs = new List<OutputVariable>();
            foreach (var output in Outputs)
            {
                node.Outputs.Add(new OutputVariable
                {
                    Name = output.Name,
                    Type = output.Type
                });
            }
            
            node.DynamicArguments = new DynamicData();
            foreach (var kvp in DynamicArguments.UserDefinedArguments)
            {
                node.DynamicArguments.UserDefinedArguments[kvp.Key] = kvp.Value;
            }
            
            node.AllowedEventModes = new List<string>(AllowedEventModes);

            if (Next != null)
            {
                node.Next = Next.Clone();
                node.Next.Previous = node;
            }

            return node;
        }
        object ICloneable.Clone() => Clone();

        public override int GetHashCode()
        {
            var hash = Name.GetHashCode() ^ ScreenPosition.GetHashCode() ^ Color.GetHashCode();

            if (Function != null)
                hash ^= Function.GetHashCode();

            Arguments.ForEach(a => { if (!string.IsNullOrEmpty(a.Value)) hash ^= a.Value.GetHashCode(); });
            
            // Include new properties in hash code with all relevant fields
            Inputs.ForEach(i => 
            { 
                if (!string.IsNullOrEmpty(i.Name)) 
                    hash ^= i.Name.GetHashCode(); 
                if (i.LinkedOutputNodeId != Guid.Empty)
                    hash ^= i.LinkedOutputNodeId.GetHashCode();
                if (!string.IsNullOrEmpty(i.LinkedOutputName))
                    hash ^= i.LinkedOutputName.GetHashCode();
            });
            Outputs.ForEach(o => 
            { 
                if (!string.IsNullOrEmpty(o.Name)) 
                    hash ^= o.Name.GetHashCode(); 
                if (!string.IsNullOrEmpty(o.Type))
                    hash ^= o.Type.GetHashCode();
            });
            foreach (var kvp in DynamicArguments.UserDefinedArguments)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    hash ^= kvp.Value.GetHashCode();
            }
            AllowedEventModes.ForEach(e => { if (!string.IsNullOrEmpty(e)) hash ^= e.GetHashCode(); });
            
            if (Next != null)
                hash ^= Next.GetHashCode();

            return hash;
        }

        public void FixArguments(NodeFunction reference)
        {
            var argumentOrder = new List<int>();
            var newArguments  = new List<TriggerNodeArgument>();

            foreach (var arg in Arguments)
            {
                int index = reference.Arguments.FindIndex(a => a.Name == arg.Name);
                if (index >= 0)
                    argumentOrder.Add(index);
            }

            for (int i = 0; i < reference.Arguments.Count; i++)
            {
                if (argumentOrder.Contains(i))
                    newArguments.Add(Arguments[argumentOrder.IndexOf(i)]);
                else
                    newArguments.Add(new TriggerNodeArgument() { Name = reference.Arguments[i].Name, Value = reference.Arguments[i].DefaultValue });
            }

            Arguments = newArguments;
        }

		public static List<TriggerNode> LinearizeNodes(List<TriggerNode> list)
		{
			var result = new List<TriggerNode>();

			foreach (var node in list)
				AddNodeToLinearizedList(node, result);

			return result;
		}

		private static void AddNodeToLinearizedList(TriggerNode node, List<TriggerNode> list)
		{
			if (!list.Contains(node))
				list.Add(node);

			if (node.Next != null)
				AddNodeToLinearizedList(node.Next, list);

			if (node is TriggerNodeCondition && (node as TriggerNodeCondition).Else != null)
				AddNodeToLinearizedList((node as TriggerNodeCondition).Else, list);
		}
	}

    // TriggerNodeAction implementation is similar to base one

    public class TriggerNodeAction : TriggerNode
    {
        
    }

    // Condition node uses function as a bool test and uses arguments to determine
    // value and operator to compare gotten value. Therefore, functions bound to
    // condition nodes must have boolean return value and accept at least two
    // arguments, one of which being a compared value and another being an operator
    // type. Else node is optional and allows to divert script to other branch.

    public class TriggerNodeCondition : TriggerNode
    {
        public TriggerNode Else { get; set; }

        public override TriggerNode Clone()
        {
            var node = new TriggerNodeCondition()
            {
                Color = Color,
                Function = Function,
                Name = Name,
                ScreenPosition = ScreenPosition
            };

            // Generate new ID for cloned node to ensure uniqueness
            node.Id = Guid.NewGuid();
            node.Arguments.AddRange(Arguments);
            
            // Clone new properties
            foreach (var input in Inputs)
            {
                node.Inputs.Add(new InputVariable
                {
                    Name = input.Name,
                    LinkedOutputNodeId = input.LinkedOutputNodeId,
                    LinkedOutputName = input.LinkedOutputName
                });
            }
            
            foreach (var output in Outputs)
            {
                node.Outputs.Add(new OutputVariable
                {
                    Name = output.Name,
                    Type = output.Type
                });
            }
            
            foreach (var kvp in DynamicArguments.UserDefinedArguments)
            {
                node.DynamicArguments.UserDefinedArguments[kvp.Key] = kvp.Value;
            }
            
            node.AllowedEventModes.AddRange(AllowedEventModes);

            if (Next != null)
            {
                node.Next = Next.Clone();
                node.Next.Previous = node;
            }

            if (Else != null)
            {
                node.Else = Else.Clone();
                node.Else.Previous = node;
            }

            return node;

        }
    }
}
