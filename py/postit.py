from flask import Flask, jsonify, request, abort
import json

app = Flask(__name__)

@app.route('/payload', methods=['POST'])
def create_task():
    if not request.json:
        abort(400)
    with open('d:\\temp\\posted.json', 'a+') as file:
        json.dump(request.json, file)
    return jsonify(request.json), 201


if __name__ == '__main__':
    app.run(debug=True)
