__import__('pkg_resources').declare_namespace(__name__)

import ns_override.base as base
import ns_override.override as override

overrides = {k: v for k, v in override.__dict__.items() if isinstance(v, type) and isinstance(base.__dict__[k], type)}

for k, v in overrides.items():
    base.__dict__[k] = v
