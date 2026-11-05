# Workflow + Rules 融合低代码工作流平台 — 产品化路线图

> 本文档记录从最小可运行 Demo 演进为成熟产品所需补强的方向，作为后续迭代的 Backlog。

## 背景 / 核心设计思想

流程设计器画节点 → 某节点类型选"规则决策"(RuleActivity) → 弹出规则配置面板（决策表/条件组）→
保存后流程 JSON 里该节点带 ruleId → 引擎执行到此节点时调用 RuleEngine.Execute(context) →
结果写回 context（共享的 Facts/上下文）→ 后续网关(Gateway)节点根据 context 字段做路由分支。

**Workflow 负责流程编排（控制流），Rules 负责决策逻辑（业务规则），通过统一的 Context 数据模型融合。**

当前 Demo 技术栈：.NET 9 + Blazor Server + BootstrapBlazor，EF Core + SQLite 持久化。

---

## 1. 引擎能力深化

- [ ] 决策表(Decision Table)：规则从单一条件树升级为表格化多行规则（类似 Excel 决策矩阵）
- [ ] 规则版本管理与灰度发布：新旧版本共存，按比例/按条件切流
- [ ] 规则测试台：输入任意 Facts，实时预览命中结果与命中路径，便于业务人员自测
- [ ] 规则优先级与冲突解决策略（多规则同时命中时如何取舍）
- [ ] 并行网关 Fork/Join：支持多分支同时执行，Join 节点等待所有分支到达
- [ ] 子流程调用（Sub-process）：流程内嵌流程，支持复用
- [ ] 循环/重试节点：失败自动重试、指定次数循环
- [ ] 定时器节点：延迟触发、定时唤醒
- [ ] 人工任务节点增强：审批人指派（人/角色/部门）、动态表单、加签/转办
- [ ] 补偿/回滚机制（Saga 模式）：流程失败时反向执行已完成步骤的补偿逻辑
- [ ] 表达式引擎升级：从自研条件树换成/引入 DynamicExpresso 或 NCalc，支持函数（dateDiff、contains、自定义扩展函数）

## 2. 设计器体验

- [ ] 真正的拖拽画布：引入 AntV X6 / Blazor.Diagrams / 前端嵌入 BPMN.js，替代当前"节点列表+连线表格"的简化形态
- [ ] 节点属性侧边栏、连线自动布局
- [ ] 撤销/重做（Undo/Redo）
- [ ] 画布缩放、小地图导航
- [ ] 流程静态校验：保存前检查孤立节点、无出边的 Gateway、循环引用等

## 3. 运行时 / 工程化（重点难点）

- [ ] **流程挂起-恢复机制**：长流程/人工审批场景的核心难点。执行到"等待类"节点（人工任务/定时器/外部事件）时持久化状态并释放线程，事件到达后从挂起点恢复，而非同步阻塞跑完全程
  - WorkflowInstance 表：InstanceId / DefinitionId+Version / Status(Running/Suspended/Completed/Faulted/Cancelled) / CurrentNodeIds / Context快照 / WaitingFor(挂起原因) / 乐观锁Version
  - PendingTask 表：人工任务待办明细（TaskId/InstanceId/NodeId/AssigneeId/FormSchema/Result）
  - 三种恢复触发：人工审批(用户操作触发 ResumeInstance) / 定时器(后台调度器如 Hangfire/Quartz 轮询触发) / 外部事件(Webhook/消息队列触发)
  - 并发一致性：乐观锁防重复恢复、状态推进与副作用同事务提交、Resuming 占位状态防重入
  - 并行网关下的 Join 计数器：记录已到达分支数 vs 期望数，最后到达的分支触发继续
- [ ] 执行历史与审计：留痕、追溯、失败重放(Replay)、失败重试
- [ ] 多租户隔离：流程/规则/实例按租户物理或逻辑隔离
- [ ] API 化：暴露 REST/gRPC 接口，支持外部系统"发起实例"、"查询状态"、"事件回调恢复"，不仅限于页面操作

## 4. 权限与治理

- [ ] 设计态权限（谁能编辑流程/规则定义）与运行态权限（谁能审批/操作任务）分离
- [ ] 流程/规则发布审批流程：配置变更本身也需要审核上线
- [ ] 操作日志与审计追踪
- [ ] 数据字典管理：统一维护 Context 中可用字段的名称/类型/说明，供设计器做类型校验与智能提示

## 5. 可观测性

- [ ] 执行监控看板：流程成功率、平均耗时、瓶颈节点分析（卡在哪个节点最多/最久）
- [ ] 告警机制：流程超时未处理、规则执行异常、实例长期挂起等场景触发告警

## 6. 扩展点

- [ ] 自定义节点类型 SDK：允许开发者插入调用外部 API、消息通知、数据库操作等节点类型，而非把所有逻辑都塞进规则引擎
- [ ] Webhook / 消息队列事件触发流程启动

---

## 当前进度

- [x] Demo 骨架搭建中（独立 session：Workflow+rules低代码demo）
  - 数据模型：WorkflowDefinition / WorkflowNode / WorkflowEdge / RuleDefinition
  - 轻量规则引擎（条件树 AND/OR + 常见运算符）
  - 简易流程执行引擎（Start→RuleActivity→Gateway→Task→End，执行日志留痕）
  - EF Core + SQLite 持久化
  - Blazor Server + BootstrapBlazor 四个页面：流程列表 / 流程设计器 / 规则库 / 流程运行&日志
  - 内置示例：请假审批流程（按请假天数是否>3天路由到总监/经理审批）

## 下一步建议优先级（讨论中）

1. **流程挂起-恢复机制**（已完成方案设计，见上文"运行时/工程化"章节）— 长流程与人工审批场景的核心技术难点，建议优先落地
2. 真正的拖拽画布 — 用户体验关键升级点
3. 其余按业务实际需要排期
