import {
  IconDelete,
  IconMore,
  IconPlayCircle,
  IconSearch,
  IconStop,
  IconUploadError,
  IconVigoLogo,
} from "@douyinfe/semi-icons";
import {
  Descriptions,
  Divider,
  Dropdown,
  Input,
  Popconfirm,
  Popover,
  Table,
  Tag,
  TextArea,
  Toast,
  Tooltip,
  Typography,
} from "@douyinfe/semi-ui";
import { Data } from "@douyinfe/semi-ui/lib/es/descriptions";
import {
  ExpandedRowRender,
  OnRow,
} from "@douyinfe/semi-ui/lib/es/table/interface";
import { useCallback, useEffect, useMemo, useState } from "react";
import useFetch from "use-http";
import { JobDetail, Scheduler, TriggerTimeline } from "../../types";
import apiconfig from "../../apiconfig";
import columns from "./columns";
import RenderValue from "./render-value";
import FlipClockCountdown from "@leenguyen/react-flip-clock-countdown";
import { dayFromNow, dayTime, formatDuration } from "../../utils";
import styles from "./index.module.css";
import clsx from "clsx";
import CurrentTime from "../current-time";

const style = {
  boxShadow: "var(--semi-shadow-elevated)",
  backgroundColor: "var(--semi-color-bg-2)",
  borderRadius: "4px",
  padding: "10px",
  margin: "10px",
  width: "350px",
};

function getOValueByData(key: string, expandData: Data[]): any {
  const item = expandData.find((u) => u.key === key) as any;
  return item?.ovalue ?? null;
}

function safeToString(value: any): string {
  return value == null ? "" : String(value);
}

export default function Jobs({ mode }: { mode: string }) {
  /**
   * 作业状态
   */
  const [jobs, setJobs] = useState<Scheduler[]>([]);
  const [words, setWords] = useState<string>("");
  const [allTimelines, setAllTimelines] = useState<TriggerTimeline[]>([]);

  const jobList = useMemo(() => {
    const trimWords = words.trim();
    if (!trimWords) return jobs;

    return jobs.filter((u) => {
      const matchJobDetail = () => {
        const jd = u.jobDetail;
        if (!jd) return false;
        return (
          jd.jobId?.includes(trimWords) ||
          jd.groupName?.includes(trimWords) ||
          jd.description?.includes(trimWords) ||
          jd.jobType?.includes(trimWords) ||
          jd.assemblyName?.includes(trimWords) ||
          jd.properties?.includes(trimWords) ||
          (jd.concurrent ? "并行" : "串行").includes(trimWords) ||
          (jd.temporary ? "临时" : "").includes(trimWords)
        );
      };

      const matchTriggers = () => {
        return (u.triggers || []).some((t) =>
          [
            t.triggerId,
            t.description,
            t.triggerType,
            t.assemblyName,
            t.args,
          ].some((val) => val?.includes(trimWords)),
        );
      };

      return matchJobDetail() || matchTriggers();
    });
  }, [jobs, words]);

  /**
   * 初始化请求配置
   */
  const { post, response } = useFetch(apiconfig.hostAddress, apiconfig.options);

  /**
   * 获取内存中所有作业
   */
  const loadJobs = useCallback(async () => {
    try {
      const data = await post("/get-jobs");
      if (response.ok) {
        setJobs(data || []);
      }
    } catch (error) {
      console.error("加载作业列表失败:", error);
      Toast.error({ content: "加载作业列表失败", duration: 3 });
    }
  }, [post, response]);

  /**
   * 获取内存中所有运行记录
   */
  const loadAllTimelines = useCallback(async () => {
    try {
      const data = await post("/timelines-log");
      if (response.ok) {
        setAllTimelines(data || []);
      }
    } catch (error) {
      console.error("加载运行记录失败:", error);
    }
  }, [post, response]);

  /**
   * 操作作业触发器
   */
  const callAction = useCallback(
    async (jobid: string, triggerid: string, action: string) => {
      try {
        const params = new URLSearchParams({ jobid, triggerid, action });
        await post(`/operate-trigger?${params.toString()}`);

        if (response.ok) {
          Toast.success({ content: "操作成功", duration: 3 });
          loadJobs();
          loadAllTimelines();
        } else {
          throw new Error(response.statusText || "请求失败");
        }
      } catch (error: any) {
        console.error("操作失败:", error);
        Toast.error({ content: error.message || "操作失败", duration: 3 });
      }
    },
    [post, response, loadJobs, loadAllTimelines],
  );

  /**
   * 生成表格类型数据
   */
  const data: JobDetail[] = useMemo(() => {
    if (!jobList?.length) return [];

    return jobList
      .filter((scheduler) => {
        if (
          apiconfig.displayEmptyTriggerJobs === "false" &&
          !scheduler.triggers?.length
        ) {
          return false;
        }
        return true;
      })
      .map((scheduler) => {
        return {
          ...scheduler.jobDetail!,
          triggers: scheduler.triggers,
          refreshDate: new Date(),
        } as JobDetail;
      });
  }, [jobList]);

  useEffect(() => {
    loadJobs();
    loadAllTimelines();

    const eventSource = new EventSource(
      `${apiconfig.hostAddress}/check-change`,
    );

    eventSource.onmessage = () => {
      loadJobs();
      loadAllTimelines();
    };

    eventSource.onerror = (err) => {
      console.warn("EventSource 连接异常:", err);
    };

    return () => {
      eventSource.close();
    };
  }, [loadJobs, loadAllTimelines]);

  /**
   * 展开行渲染
   */
  const expandRowRender: ExpandedRowRender<JobDetail> = useCallback(
    (jobDetail) => {
      const scheduler = jobList.find(
        (u) => u.jobDetail?.jobId === jobDetail?.jobId,
      );
      if (!scheduler) return null;

      // 构建触发器列表
      const triggerData: Data[][] =
        scheduler.triggers?.map((trigger) => {
          return Object.entries(trigger).map(
            ([prop, value]) =>
              ({
                key: prop.charAt(0).toUpperCase() + prop.slice(1),
                value: (
                  <RenderValue prop={prop} value={value} trigger={trigger} />
                ),
                ovalue: value,
              }) as Data,
          );
        }) || [];

      return (
        <div style={{ display: "flex", flexWrap: "wrap" }}>
          {triggerData.map((expandData, index) => {
            const triggerId = safeToString(
              getOValueByData("TriggerId", expandData),
            );
            const jobId = safeToString(getOValueByData("JobId", expandData));

            return (
              <div style={style} key={`${jobId}_${triggerId}_${index}`}>
                <div
                  style={{
                    marginTop: 3,
                    marginRight: 5,
                    marginLeft: 5,
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "flex-start",
                  }}
                >
                  {Number(getOValueByData("Status", expandData)) === 3 ? (
                    <Tooltip content="启动">
                      <IconPlayCircle
                        style={{ color: "red", cursor: "pointer" }}
                        size="large"
                        onClick={() =>
                          callAction(
                            safeToString(getOValueByData("JobId", expandData)),
                            safeToString(
                              getOValueByData("TriggerId", expandData),
                            ),
                            "start",
                          )
                        }
                      />
                    </Tooltip>
                  ) : (
                    <span></span>
                  )}

                  <FlipClockCountdown
                    to={getOValueByData("NextRunTime", expandData)}
                    labels={["天", "时", "分", "秒"]}
                    labelStyle={{
                      fontSize: 12,
                      fontWeight: 500,
                      color: "var(--semi-color-text-0)",
                    }}
                    digitBlockStyle={{ width: 20, height: 30, fontSize: 15 }}
                    hideOnComplete={false}
                  />

                  <Dropdown
                    render={
                      <Dropdown.Menu>
                        <Dropdown.Item
                          onClick={() =>
                            callAction(
                              safeToString(
                                getOValueByData("JobId", expandData),
                              ),
                              safeToString(
                                getOValueByData("TriggerId", expandData),
                              ),
                              "start",
                            )
                          }
                        >
                          <IconPlayCircle size="extra-large" /> 启动
                        </Dropdown.Item>
                        <Dropdown.Item
                          onClick={() =>
                            callAction(
                              safeToString(
                                getOValueByData("JobId", expandData),
                              ),
                              safeToString(
                                getOValueByData("TriggerId", expandData),
                              ),
                              "pause",
                            )
                          }
                        >
                          <IconStop size="extra-large" /> 暂停
                        </Dropdown.Item>
                        <Dropdown.Item>
                          <Popconfirm
                            zIndex={10000000}
                            title={`确定要删除当前触发器 [${safeToString(
                              getOValueByData("TriggerId", expandData),
                            )}]？`}
                            onConfirm={() =>
                              callAction(
                                safeToString(
                                  getOValueByData("JobId", expandData),
                                ),
                                safeToString(
                                  getOValueByData("TriggerId", expandData),
                                ),
                                "remove",
                              )
                            }
                          >
                            <IconDelete size="small" /> &nbsp;删除
                          </Popconfirm>
                        </Dropdown.Item>
                        <Dropdown.Item
                          onClick={() =>
                            callAction(
                              safeToString(
                                getOValueByData("JobId", expandData),
                              ),
                              safeToString(
                                getOValueByData("TriggerId", expandData),
                              ),
                              "run",
                            )
                          }
                        >
                          <IconVigoLogo size="extra-large" /> 手动执行
                        </Dropdown.Item>
                      </Dropdown.Menu>
                    }
                  >
                    <IconMore style={{ cursor: "pointer" }} size="large" />
                  </Dropdown>
                </div>

                <Divider margin="8px" />
                <Descriptions align="left" data={expandData} />
              </div>
            );
          })}
        </div>
      );
    },
    [jobList, callAction],
  );

  const handleRow: OnRow<JobDetail> = (_, index = 0) => {
    if (index % 2 === 0) {
      return {
        style: {
          background: "var(--semi-color-fill-0)",
        },
      };
    }
    return {};
  };

  const invalidJobCount = data.filter((u) => !u.triggers?.length).length;

  return (
    <>
      <div
        style={{
          border: "1px solid var(--semi-color-border)",
          borderRadius: "10px",
        }}
      >
        <Input
          prefix={<IconSearch />}
          showClear
          placeholder="搜索关键字..."
          value={words}
          onChange={(val) => setWords(val || "")}
          autoFocus
        />

        <Table
          rowKey="jobId"
          columns={columns}
          dataSource={data}
          onRow={handleRow}
          expandedRowRender={expandRowRender}
          pagination={false}
          resizable
          bordered
          expandRowByClick
          expandAllRows={apiconfig.defaultExpandAllJobs === "true"}
          rowExpandable={(jobDetail) =>
            !!(
              jobDetail?.jobId &&
              jobList.find((u) => u.jobDetail?.jobId === jobDetail?.jobId)
                ?.triggers?.length
            )
          }
        />
        <Typography.Paragraph type="secondary" style={{ padding: 10 }}>
          {words.trim().length > 0 ? (
            <>
              搜索 "<b>{words.trim()}</b>" 共 <b>{jobList.length}</b> 项结果。
            </>
          ) : (
            <>
              共有 <b>{data.length}</b> 项作业任务
              {invalidJobCount > 0 && (
                <>
                  ，其中 <b>{invalidJobCount}</b> 项未设置触发器
                </>
              )}
              。
            </>
          )}
        </Typography.Paragraph>
      </div>
      {words.trim().length === 0 && (
        <div>
          <Typography.Title heading={5} style={{ margin: "24px 0 16px 0" }}>
            # 运行记录
          </Typography.Title>
          <div style={{ color: "var(--semi-color-text-0)" }}>
            {allTimelines.map((timeline, i) => (
              <div
                key={`${timeline.jobId || ""}_${timeline.triggerId || ""}_${i}`}
                style={{ marginBottom: 8, fontSize: 14 }}
                className={clsx(
                  styles.timelineItem,
                  mode === "dark" && styles.dark,
                )}
              >
                <Tag size="large" color="green" type="light">
                  {timeline.jobId}
                </Tag>{" "}
                <Tag size="large" color="green" type="light">
                  {timeline.triggerId}
                </Tag>{" "}
                <Tag color="grey" type="light">
                  {dayTime(timeline.lastRunTime).format("YYYY/MM/DD HH:mm:ss")}(
                  {dayFromNow(timeline.lastRunTime)})
                </Tag>{" "}
                第{" "}
                <Tag color="green" type="light">
                  {timeline.numberOfRuns}
                </Tag>{" "}
                次运行，耗时{" "}
                <Tooltip
                  content={`${timeline.elapsedTime}ms`}
                  zIndex={10000000001}
                >
                  <span>
                    <Tag color="lime" type="light">
                      {formatDuration(timeline.elapsedTime || 0)}
                    </Tag>
                  </span>
                </Tooltip>{" "}
                {timeline.mode === 1 && (
                  <Tag color="yellow" type="solid">
                    手动
                  </Tag>
                )}
                {timeline.exception && (
                  <Popover
                    showArrow
                    content={
                      <div
                        className="exception-box"
                        style={{ padding: 10, width: 400 }}
                      >
                        <TextArea value={timeline.exception} rows={10} />
                      </div>
                    }
                    trigger="click"
                    zIndex={10000000002}
                  >
                    <IconUploadError
                      style={{
                        position: "relative",
                        color: "red",
                        top: 4,
                        cursor: "pointer",
                        marginLeft: 5,
                      }}
                    />
                  </Popover>
                )}
                {timeline.result && (
                  <div>
                    <Typography.Paragraph
                      ellipsis={{
                        rows: 2,
                        expandable: true,
                        expandText: "展开",
                        collapsible: true,
                        collapseText: "折叠",
                      }}
                      style={{ width: 200 }}
                      copyable
                    >
                      {timeline.result}
                    </Typography.Paragraph>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </>
  );
}
