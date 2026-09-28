FROM docker:29-cli@sha256:018edbc908e08fcc9dbf029c812c34251e9b4719e6f71ca0e5eae2a987d014ca AS dockercli
FROM jenkins/inbound-agent:jdk21@sha256:6a31d728c22ad74adbb6b9a1a210eb9ec2599f644b95edd7f8a3b8a71d20772c
USER root
COPY --from=dockercli /usr/local/bin/docker /usr/local/bin/docker
COPY --from=dockercli /usr/local/libexec/docker/cli-plugins/docker-buildx /usr/local/libexec/docker/cli-plugins/docker-buildx
USER jenkins
ENTRYPOINT ["java", "-jar", "/usr/share/jenkins/agent.jar", "-url", "http://smartwms-jenkins:8080/", "-name", "smartwms-local", "-secret", "@/run/secrets/jenkins_agent_secret", "-webSocket", "-workDir", "/home/jenkins/agent"]
