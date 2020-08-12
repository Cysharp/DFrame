import random
from locust import HttpUser, task, between

class MyUser(HttpUser):
    wait_time = between(0, 1)

    @task(20)
    def index(self):
        self.client.get("/")
