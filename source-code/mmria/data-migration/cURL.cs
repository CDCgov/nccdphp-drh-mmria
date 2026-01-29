// Type alias for canonical cURL in mmria.getset namespace
// This allows code in the migrate namespace to use cURL without changing all usages
using global::mmria.getset;

namespace migrate
{
    using cURL = global::mmria.getset.cURL;
}
