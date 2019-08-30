#pragma once

#include <cstdlib>
#include <cmath>

#include "../utils/adBuffer.h"
#include "../../../shared/defs.h"

// GMM function differentiated in reverse mode by Tapenade.
void gmm_objective_b(
    int d,
    int k,
    int n,
    double const* alphas,
    double* alphasb,
    double const* means,
    double* meansb,
    double const* icf,
    double* icfb,
    double const* x,
    Wishart wishart,
    double* err,
    double* errb
);