#!/bin/bash
# Test script to verify Docker-in-Docker functionality in the devcontainer

set -e

echo "======================================"
echo "Docker-in-Docker Functionality Test"
echo "======================================"
echo ""

# Test 1: Check if Docker is available
echo "Test 1: Checking if Docker is available..."
if command -v docker &> /dev/null; then
    docker --version
    echo "✓ Docker is available"
else
    echo "✗ Docker is not available"
    exit 1
fi
echo ""

# Test 2: Check Docker daemon is running
echo "Test 2: Checking if Docker daemon is running..."
if docker info &> /dev/null; then
    echo "✓ Docker daemon is running"
else
    echo "✗ Docker daemon is not running"
    exit 1
fi
echo ""

# Test 3: Run a simple container
echo "Test 3: Running hello-world container..."
if docker run --rm hello-world &> /dev/null; then
    echo "✓ Successfully ran hello-world container"
else
    echo "✗ Failed to run hello-world container"
    exit 1
fi
echo ""

# Test 4: Build a test image
echo "Test 4: Building a test Docker image..."
TEST_DIR=$(mktemp -d)
cat > "$TEST_DIR/Dockerfile" << 'EOF'
FROM alpine:latest
CMD ["echo", "Docker-in-Docker is working!"]
EOF

if docker build -t dind-test -f "$TEST_DIR/Dockerfile" "$TEST_DIR" &> /dev/null; then
    echo "✓ Successfully built test image"
else
    echo "✗ Failed to build test image"
    rm -rf "$TEST_DIR"
    exit 1
fi
echo ""

# Test 5: Run the built image
echo "Test 5: Running the built test image..."
OUTPUT=$(docker run --rm dind-test)
if [ "$OUTPUT" = "Docker-in-Docker is working!" ]; then
    echo "✓ Test image ran successfully"
    echo "  Output: $OUTPUT"
else
    echo "✗ Test image output was unexpected"
    echo "  Expected: 'Docker-in-Docker is working!'"
    echo "  Got: '$OUTPUT'"
    rm -rf "$TEST_DIR"
    exit 1
fi
echo ""

# Test 6: Clean up test image
echo "Test 6: Cleaning up test image..."
if docker rmi dind-test &> /dev/null; then
    echo "✓ Successfully removed test image"
else
    echo "✗ Failed to remove test image"
fi
rm -rf "$TEST_DIR"
echo ""

# Test 7: Check Docker Compose
echo "Test 7: Checking Docker Compose availability..."
if docker compose version &> /dev/null; then
    docker compose version
    echo "✓ Docker Compose is available"
else
    echo "⚠ Docker Compose is not available (optional)"
fi
echo ""

echo "======================================"
echo "All Docker-in-Docker tests passed! ✓"
echo "======================================"
echo ""
echo "You can now use Docker commands within this devcontainer to:"
echo "  - Build and test Docker images"
echo "  - Run containerized applications"
echo "  - Test multi-container deployments"
echo ""
