from locust import task, FastHttpUser

class MyUser(FastHttpUser):
    @task
    def index(self):
        response = self.client.get("/")