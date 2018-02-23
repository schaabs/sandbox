from flask import Flask, jsonify, request, abort

app = Flask(__name__)

@app.route('/payload', methods=['POST'])
def create_task():
    if not request.json:
        abort(400)
        with open('d:\\temp\\posted.json', 'wa+') as file:
            file.write(jsonify(request.json))
        jsonify(request.json)
    return jsonify(request.json), 201


if __name__ == '__main__':
    app.run(debug=True)
