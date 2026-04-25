using gg_shader::f32::{vec3, vec4}

shader mesh_default {
    varying v_color: vec3,

    [vertex_main]
    vs_main(position: vec3, color: vec3) -> vec4 {
        v_color = color
        return vec4(position, 1.0)
    }

    [fragment_main]
    fs_main() -> vec4 {
        return vec4(v_color, 1.0)
    }
}
