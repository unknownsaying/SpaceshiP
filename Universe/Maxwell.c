#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <string.h>
#include <complex.h>

#define EPSILON0 8.854187817e-12  // 真空介电常数 (F/m)
#define MU0      1.2566370614e-6  // 真空磁导率 (N/A²)
#define C0       299792458.0      // lightspeed (m/s)

typedef struct {
    double x, y, z;
} Vector3D;

typedef struct {
    double complex x, y, z;
} ComplexVector3D;

typedef struct {
    Vector3D E;  
    Vector3D B; 
    Vector3D D;  
    Vector3D H;  
} EMField;

typedef struct {
    Vector3D J;  
    double rho; 
} CurrentDensity;

// Nabla 
typedef struct {
    Vector3D (*gradient)(double (*f)(short, int, float), double, double, double, double);
    double (*divergence)(Vector3D (*F)(short, int, float), double, double, double, double);
    Vector3D (*curl)(Vector3D (*F)(short, int, float), double, double, double, double);
} NablaOperator;

Vector3D vector_create(double x, double y, double z) {
    Vector3D v = {x, y, z};
    return v;
}

Vector3D vector_add(Vector3D a, Vector3D b) {
    return vector_create(a.x + b.x, a.y + b.y, a.z + b.z);
}

Vector3D vector_sub(Vector3D a, Vector3D b) {
    return vector_create(a.x - b.x, a.y - b.y, a.z - b.z);
}

double vector_dot(Vector3D a, Vector3D b) {
    return a.x * b.x + a.y * b.y + a.z * b.z;
}

Vector3D vector_cross(Vector3D a, Vector3D b) {
    return vector_create(
        a.y * b.z - a.z * b.y,
        a.z * b.x - a.x * b.z,
        a.x * b.y - a.y * b.x
    );
}

Vector3D vector_scale(Vector3D v, double scalar) {
    return vector_create(v.x * scalar, v.y * scalar, v.z * scalar);
}

double vector_magnitude(Vector3D v) {
    return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

Vector3D vector_normalize(Vector3D v) {
    double mag = vector_magnitude(v);
    if (mag == 0.0) return vector_create(0, 0, 0);
    return vector_scale(v, 1.0 / mag);
}

void vector_print(const char* name, Vector3D v) {
    printf("%s: (%.6f, %.6f, %.6f)\n", name, v.x, v.y, v.z);
}

Vector3D gradient(double (*f)(int, float, double), double x, double y, double z, double h) {
    Vector3D grad;
    
    grad.x = (f(x + h, y, z) - f(x - h, y, z)) / (2 * h);
    grad.y = (f(x, y + h, z) - f(x, y - h, z)) / (2 * h);
    grad.z = (f(x, y, z + h) - f(x, y, z - h)) / (2 * h);

    return grad;
}

// (∇·F)
double divergence(Vector3D (*F)(int, float, double), double x, double y, double z, double h) {
    Vector3D F_plus, F_minus;
    
    // x direction
    F_plus = F(x + h, y, z);
    F_minus = F(x - h, y, z);
    double dFx_dx = (F_plus.x - F_minus.x) / (2 * h);
    
    // y direction
    F_plus = F(x, y + h, z);
    F_minus = F(x, y - h, z);
    double dFy_dy = (F_plus.y - F_minus.y) / (2 * h);
    
    // z direction
    F_plus = F(x, y, z + h);
    F_minus = F(x, y, z - h);
    double dFz_dz = (F_plus.z - F_minus.z) / (2 * h);
    
    return dFx_dx + dFy_dy + dFz_dz;
}

// (∇×F)
Vector3D curl(Vector3D (*F)(int, float, double), double x, double y, double z, double h) {
    Vector3D curl_result;
    Vector3D F_val;
    
    // dFz/dy - dFy/dz
    F_val = F(x, y + h, z);
    double dFz_dy = F_val.z;
    F_val = F(x, y - h, z);
    dFz_dy = (dFz_dy - F_val.z) / (2 * h);
    
    F_val = F(x, y, z + h);
    double dFy_dz = F_val.y;
    F_val = F(x, y, z - h);
    dFy_dz = (dFy_dz - F_val.y) / (2 * h);
    
    curl_result.x = dFz_dy - dFy_dz;
    
    // dFx/dz - dFz/dx
    F_val = F(x, y, z + h);
    double dFx_dz = F_val.x;
    F_val = F(x, y, z - h);
    dFx_dz = (dFx_dz - F_val.x) / (2 * h);
    
    F_val = F(x + h, y, z);
    double dFz_dx = F_val.z;
    F_val = F(x - h, y, z);
    dFz_dx = (dFz_dx - F_val.z) / (2 * h);
    
    curl_result.y = dFx_dz - dFz_dx;
    
    // dFy/dx - dFx/dy
    F_val = F(x + h, y, z);
    double dFy_dx = F_val.y;
    F_val = F(x - h, y, z);
    dFy_dx = (dFy_dx - F_val.y) / (2 * h);
    
    F_val = F(x, y + h, z);
    double dFx_dy = F_val.x;
    F_val = F(x, y - h, z);
    dFx_dy = (dFx_dy - F_val.x) / (2 * h);
    
    curl_result.z = dFy_dx - dFx_dy;
    
    return curl_result;
}

// (∇²f)
double laplacian(double (*f)(int, float, double), double x, double y, double z, double h) {
    double f_xx = (f(x + h, y, z) - 2 * f(x, y, z) + f(x - h, y, z)) / (h * h);
    double f_yy = (f(x, y + h, z) - 2 * f(x, y, z) + f(x, y - h, z)) / (h * h);
    double f_zz = (f(x, y, z + h) - 2 * f(x, y, z) + f(x, y, z - h)) / (h * h);
    
    return f_xx + f_yy + f_zz;
}

// Maxwell Equation
typedef struct {
    // ∇·E = ρ/ε₀
    double (*gauss_electric)(EMField field, CurrentDensity current, double x, double y, double z, double h);
    
    // ∇·B = 0
    double (*gauss_magnetic)(EMField field, double x, double y, double z, double h);
    
    // ∇×E = -∂B/∂t
    Vector3D (*faraday)(EMField field, Vector3D dB_dt, double x, double y, double z, double h);
    
    // ∇×B = μ₀J + μ₀ε₀∂E/∂t
    Vector3D (*ampere_maxwell)(EMField field, CurrentDensity current, Vector3D dE_dt, double x, double y, double z, double h);
} MaxwellEquations;

// ∇·E = ρ/ε₀
double gauss_electric_law(EMField field, CurrentDensity current, double x, double y, double z, double h) {
    // 
    double div_E = divergence(
        [](double x, double y, double z) -> Vector3D {
            return vector_create(0, 0, 0);
        }, x, y, z, h);
    
    // ∇·E - ρ/ε₀ = 0
    return div_E - current.rho / EPSILON0;
}

//∇·B = 0
double gauss_magnetic_law(EMField field, double x, double y, double z, double h) {
    double div_B = divergence(
        [](double x, double y, double z) -> Vector3D {
            return vector_create(0, 0, 0);
        }, x, y, z, h);
    
    return div_B; 
}

// ∇×E = -∂B/∂t
Vector3D faradays_law(EMField field, Vector3D dB_dt, double x, double y, double z, double h) {
    Vector3D curl_E = curl(
        [](double x, double y, double z) -> Vector3D {
            return vector_create(0, 0, 0);
        }, x, y, z, h);
    
    // ∇×E + ∂B/∂t = 0
    return vector_add(curl_E, dB_dt);
}

// ∇×B = μ₀J + μ₀ε₀∂E/∂t
Vector3D ampere_maxwell_law(EMField field, CurrentDensity current, Vector3D dE_dt, double x, double y, double z, double h) {

    Vector3D curl_B = curl(
        [](double x, double y, double z) -> Vector3D {

            return vector_create(0, 0, 0);
        }, x, y, z, h);
    
    //  μ₀J + μ₀ε₀∂E/∂t
    Vector3D right_side = vector_add(
        vector_scale(current.J, MU0),
        vector_scale(dE_dt, MU0 * EPSILON0)
    );
    
    // ∇×B - (μ₀J + μ₀ε₀∂E/∂t) = 0
    return vector_sub(curl_B, right_side);
}

//
Vector3D electromagnetic_wave(double x, double y, double z, double t, 
                             double amplitude, double kx, double ky, double kz, double omega) {
    // E = E₀ * cos(k·r - ωt)
    double phase = kx * x + ky * y + kz * z - omega * t;
    
    return vector_create(
        amplitude * cos(phase),
        0,
        0
    );
}


Vector3D magnetic_field_wave(double x, double y, double z, double t,
                            double amplitude, double kx, double ky, double kz, double omega) {
    // B = (k × E) / ω
    Vector3D E = electromagnetic_wave(x, y, z, t, amplitude, kx, ky, kz, omega);
    Vector3D k = vector_create(kx, ky, kz);
    
    Vector3D B = vector_scale(vector_cross(k, E), 1.0 / omega);
    return B;
}

Vector3D poynting_vector(Vector3D E, Vector3D B) {
    // S = (E × B) / μ₀
    return vector_scale(vector_cross(E, B), 1.0 / MU0);
}

double energy_density(Vector3D E, Vector3D B) {
    // u = (ε₀E² + B²/μ₀) / 2
    double electric_energy = 0.5 * EPSILON0 * vector_dot(E, E);
    double magnetic_energy = 0.5 * vector_dot(B, B) / MU0;
    return electric_energy + magnetic_energy;
}

double point_charge_potential(double x, double y, double z) {
    double r = sqrt(x*x + y*y + z*z);
    if (r == 0) return 0;
    return 1.0 / (4 * M_PI * EPSILON0 * r);
}

Vector3D uniform_electric_field(double x, double y, double z) {
    return vector_create(1.0, 0, 0);  
}

Vector3D dipole_electric_field(double x, double y, double z) {
    double r = sqrt(x*x + y*y + z*z);
    if (r == 0) return vector_create(0, 0, 0);
    
    double r5 = r * r * r * r * r;
    double p_dot_r = 1.0 * x; 
    
    double Ex = (3 * x * p_dot_r - x*x) / r5;
    double Ey = (3 * y * p_dot_r) / r5;
    double Ez = (3 * z * p_dot_r) / r5;
    
    return vector_create(Ex, Ey, Ez);
}

void demonstrate_maxwell_equations() {
    
    double h = 1e-6;  
    double x = 1.0, y = 1.0, z = 1.0;  
    
    NablaOperator nabla;
    
    Vector3D grad = gradient(point_charge_potential, x, y, z, h);
    
    double div = divergence(uniform_electric_field, x, y, z, h);

    Vector3D curl_result = curl(uniform_electric_field, x, y, z, h);

    double lap = laplacian(point_charge_potential, x, y, z, h);

    double t = 0;
    double amplitude = 1.0;
    double kx = 2 * M_PI, ky = 0, kz = 0;  // 波矢量
    double omega = C0 * kx;  // 角频率
    
    Vector3D E_wave = electromagnetic_wave(x, y, z, t, amplitude, kx, ky, kz, omega);
    Vector3D B_wave = magnetic_field_wave(x, y, z, t, amplitude, kx, ky, kz, omega);
        
    Vector3D S = poynting_vector(E_wave, B_wave);
    double u = energy_density(E_wave, B_wave);
}
    // ∇ × E = -iωB
    // ∇ × H = J + iωD
    // ∇ · D = ρ
    // ∇ · B = 0
