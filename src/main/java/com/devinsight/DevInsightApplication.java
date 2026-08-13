package com.devinsight;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * Entry point for the DevInsight application.
 *
 * DevInsight is built using Hexagonal Architecture (Ports & Adapters) to ensure
 * a clean separation between business logic and infrastructure concerns.
 *
 * Package structure:
 *   domain/model     - Core domain entities and value objects
 *   domain/port/in   - Input ports (use case interfaces driven by the outside world)
 *   domain/port/out  - Output ports (interfaces the domain uses to reach infrastructure)
 *   domain/service   - Domain service implementations of input ports
 *   adapter/in/rest  - REST controllers (primary/driving adapters)
 *   adapter/out      - GitHub, JPA adapters (secondary/driven adapters)
 *   infrastructure   - Spring configuration, security, JWT, etc.
 */
@SpringBootApplication
public class DevInsightApplication {

    public static void main(String[] args) {
        SpringApplication.run(DevInsightApplication.class, args);
    }
}
