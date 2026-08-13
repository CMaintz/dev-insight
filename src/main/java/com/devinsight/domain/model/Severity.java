package com.devinsight.domain.model;

/**
 * Indicates the importance / urgency of a feedback item.
 *
 * LOW    - Informational; nice-to-have improvement.
 * MEDIUM - Noticeable gap that should be addressed.
 * HIGH   - Critical issue that significantly impacts the score or portfolio quality.
 */
public enum Severity {
    LOW,
    MEDIUM,
    HIGH
}
