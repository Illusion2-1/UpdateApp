## UpdateApp

使用JSON序列化数据和Http实现简单增量更新。  
JSON序列化数据由UpdateServer提供，客户端（UpdateApp）在收到数据后向资源服务器请求资源。
用Task托管线程池实现简单并发；口令加盐生成动态令牌

- UpdateServer 在服务器的更新内容提供程序，提供序列化后的JSON数据
- UpdateApp 在客户端的增量更新程序
- SimpleHashProcessor Debug用，生成动态令牌

**UpdateServer没有实现资源服务器功能，配合nginx, apache, caddy, simplehttpserver等使用**