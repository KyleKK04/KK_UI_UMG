using System;
using System.Collections.Generic;

namespace KK.UI.UMG.Binding
{
    public sealed class UguiBinder
    {
        private readonly Dictionary<string, ViewModelBinding> _bindingsByField = new Dictionary<string, ViewModelBinding>();
        private readonly Dictionary<string, object> _controlsById = new Dictionary<string, object>();

        public void Configure(UIViewBase view, IReadOnlyList<ViewModelBinding> bindings)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            _bindingsByField.Clear();
            _controlsById.Clear();

            var viewType = view.GetType();
            foreach (var binding in bindings)
            {
                if (binding.Mode != BindingMode.OneWay)
                {
                    throw new NotSupportedException($"Binding mode '{binding.Mode}' is not supported in v1.0.");
                }

                var property = viewType.GetProperty(binding.ControlId) ?? viewType.GetProperty(ToPascal(binding.ControlId));
                if (property == null)
                {
                    throw new InvalidOperationException($"View '{viewType.Name}' does not expose control '{binding.ControlId}'.");
                }

                _bindingsByField[binding.FieldId] = binding;
                _controlsById[binding.ControlId] = property.GetValue(view);
            }
        }

        public void Flush(ViewModelStore store)
        {
            foreach (var dirty in store.TakeDirty())
            {
                if (!_bindingsByField.TryGetValue(dirty.FieldId, out var binding))
                {
                    continue;
                }

                if (!_controlsById.TryGetValue(binding.ControlId, out var control))
                {
                    continue;
                }

                UguiApplyHelper.ApplyOrThrow(control, binding.Property, dirty.Value);
            }
        }

        private static string ToPascal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}
