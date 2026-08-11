import {
  ActionIcon,
  Alert,
  Badge,
  Box,
  Button,
  Card,
  Divider,
  Group,
  Modal,
  Paper,
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  Tree,
  Tooltip,
  SegmentedControl,
  useTree,
} from '@mantine/core';
import {
  IconArrowDown,
  IconArrowLeft,
  IconDeviceFloppy,
  IconEdit,
  IconHomePlus,
  IconPlus,
  IconTrash,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import cardHoverClasses from '@/styles/card-hover.module.css';
import { FlowIconKey, FlowMenuItem, Flow, MenuNodeType } from '@/api/interfaces/Flow/Flows';
import { flowsApi } from '@/api/FlowsApi';
import { queuesApi } from '@/api/QueuesApi';
import { useActiveLocation } from '@/features/locations/LocationContext';
import { PageHeader } from '@/components/PageHeader/PageHeader';

interface IconOption {
  key: FlowIconKey;
  labelKey: string;
  emoji: string;
}

const iconOptions: IconOption[] = [
  { key: 'ticket', labelKey: 'flows.iconTicket', emoji: '🎫' },
  { key: 'fruit', labelKey: 'flows.iconFruit', emoji: '🍎' },
  { key: 'fish', labelKey: 'flows.iconFish', emoji: '🐟' },
  { key: 'meat', labelKey: 'flows.iconMeat', emoji: '🥩' },
  { key: 'car', labelKey: 'flows.iconCar', emoji: '🚗' },
];

function createId() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `id-${Math.random().toString(36).slice(2)}`;
}

function getNodeEmoji(key: FlowIconKey) {
  return iconOptions.find((option) => option.key === key)?.emoji ?? '🏠';
}

function getFlowModeLabelKey(flowType: number): string {
  if (flowType === 1) {
    return 'flows.SetFree';
  }
  if (flowType === 2) {
    return 'flows.ManualCall';
  }
  return 'flows.TicketMachine';
}

function getChildren(items: FlowMenuItem[], parentId: string | null) {
  return items.filter((item) => item.parentId === parentId && !item.removedAt);
}

function collectDescendantIds(items: FlowMenuItem[], nodeId: string): string[] {
  const directChildren = items.filter((item) => item.parentId === nodeId).map((item) => item.id);
  return directChildren.flatMap((childId) => [childId, ...collectDescendantIds(items, childId)]);
}

function collectTreeValues(nodes: Tree.NodeData[]): string[] {
  return nodes.flatMap((node) => [node.value, ...collectTreeValues(node.children ?? [])]);
}

export function FlowsPanel() {
  const location = useActiveLocation();
  const { t } = useTranslation();
  const tree = useTree();

  const [flows, setFlows] = useState<Flow[]>([]);
  const [activeFlowId, setActiveFlowId] = useState<string | null>(null);
  const [ruleError, setRuleError] = useState<string | null>(null);
  const [flowModalOpened, setFlowModalOpened] = useState(false);
  const [editingFlowId, setEditingFlowId] = useState<string | null>(null);
  const [flowName, setFlowName] = useState('');
  const [flowDescription, setFlowDescription] = useState('');
  const [flowMode, setFlowMode] = useState<number>(0);
  const [pendingSelectedNodeId, setPendingSelectedNodeId] = useState<string | null>(null);
  const [menuItems, setMenuItems] = useState<FlowMenuItem[]>([]);
  const [deletingFlow, setDeletingFlow] = useState<Flow | undefined>();
  const [unsavedModalOpened, setUnsavedModalOpened] = useState(false);
  const flowsRef = useRef(flows);
  flowsRef.current = flows;
  const [savedMenuItemsJson, setSavedMenuItemsJson] = useState('');

  useEffect(() => {
    if (!activeFlowId) {
      return;
    }

    const flow = flowsRef.current.find((entry) => entry.id === activeFlowId);
    setSavedMenuItemsJson(flow ? JSON.stringify(flow.menuItems) : '');
  }, [activeFlowId]);

  useEffect(() => {
    const fetchFlows = async () => {
      if (!location) {
        return;
      }

      const persistedFlows = await flowsApi.list(location.id).then((response) => response.data);
      setFlows(persistedFlows);
      setActiveFlowId(null);
    };

    fetchFlows();
  }, [location]);

  const activeFlow = useMemo(
    () => flows.find((flow) => flow.id === activeFlowId),
    [flows, activeFlowId]
  );

  const treeData = useMemo(() => {
    if (!activeFlow) {
      return [];
    }

    const build = (parentId: string | null): Tree.NodeData[] => {
      return getChildren(activeFlow.menuItems, parentId).map((item) => ({
        value: item.id,
        label: item.name,
        children: build(item.id),
      }));
    };

    return build(null);
  }, [activeFlow]);

  //De listTicketTypes quiero guardar los tipos de ticket para mostrarlos en el select de ticketTypeId
  const [QueueOptions, setQueueOptions] = useState<{ value: string; label: string }[]>([]);

  useEffect(() => {
    if (!activeFlow) {
      setQueueOptions([]);
      return;
    }

    const loadQueueTypes = async () => {
      try {
        if (!location) {
          setQueueOptions([]);
          return;
        }

        const types = await queuesApi.list(location.id);

        setQueueOptions(
          types.data.map((type) => ({
            value: type.id,
            label: type.name,
          }))
        );
      } catch {
        setQueueOptions([]);
      }
    };

    loadQueueTypes();
  }, [activeFlow]);

  const activeItemsById = useMemo(() => {
    if (!activeFlow) {
      return new Map<string, FlowMenuItem>();
    }

    return new Map(activeFlow.menuItems.map((item) => [item.id, item]));
  }, [activeFlow]);

  useEffect(() => {
    if (treeData.length === 0) {
      tree.clearSelected();
      setPendingSelectedNodeId(null);
      return;
    }

    const values = collectTreeValues(treeData);
    const expandedState = Object.fromEntries(values.map((value) => [value, true]));
    tree.setExpandedState(expandedState);

    if (pendingSelectedNodeId && values.includes(pendingSelectedNodeId)) {
      tree.select(pendingSelectedNodeId);
      setPendingSelectedNodeId(null);
      return;
    }

    const selected = tree.selectedState[0];
    if (!selected || !values.includes(selected)) {
      tree.select(values[0]);
    }
  }, [treeData, pendingSelectedNodeId]);

  const selectedNodeId = tree.selectedState[0] ?? null;

  const selectedNode = useMemo(() => {
    if (!activeFlow || !selectedNodeId) {
      return undefined;
    }

    return activeFlow.menuItems.find((item) => item.id === selectedNodeId);
  }, [activeFlow, selectedNodeId]);

  const selectedNodeChildrenCount = useMemo(() => {
    if (!activeFlow || !selectedNode) {
      return 0;
    }

    return getChildren(activeFlow.menuItems, selectedNode.id).length;
  }, [activeFlow, selectedNode]);

  const canCreateChild = selectedNode?.nodeType === 'menu';

  const hasUnsavedChanges =
    Boolean(activeFlow) && savedMenuItemsJson !== JSON.stringify(activeFlow?.menuItems);

  const parentOptions = useMemo(() => {
    if (!activeFlow || !selectedNode) {
      return [{ value: '', label: t('flows.rootMenu') }];
    }

    const forbiddenIds = new Set([
      selectedNode.id,
      ...collectDescendantIds(activeFlow.menuItems, selectedNode.id),
    ]);

    const options = activeFlow.menuItems
      .filter((item) => item.nodeType === 'menu' && !forbiddenIds.has(item.id))
      .map((item) => ({ value: item.id, label: item.name }));

    return [{ value: '', label: t('flows.rootMenu') }, ...options];
  }, [activeFlow, selectedNode, t]);

  function updateActiveFlow(mutator: (flow: Flow) => Flow) {
    if (!activeFlowId) {
      return;
    }

    setFlows((previous) =>
      previous.map((flow) => (flow.id === activeFlowId ? mutator(flow) : flow))
    );
  }

  function openCreateFlowModal() {
    setEditingFlowId(null);
    setFlowName('');
    setFlowDescription('');
    setFlowMode(0);
    setFlowModalOpened(true);
    setMenuItems([]);
  }

  function openEditFlowModal(flowToEdit?: Flow) {
    const flow = flowToEdit ?? activeFlow;
    if (!flow) {
      return;
    }

    setEditingFlowId(flow.id);
    setFlowName(flow.name);
    setFlowDescription(flow.description);
    setFlowMode(flow.flowType);
    setFlowModalOpened(true);
    setMenuItems(flow.menuItems);
  }

  function submitFlow() {
    const normalizedName = flowName.trim();

    //Mando a la api el flujo para guardarlo

    if (editingFlowId) {
      const flowJson = menuItems.map((item) => ({
        id: item.id,
        name: item.name,
        description: item.description,
        nodeType: item.nodeType,
        parentId: item.parentId,
        queueId: item.queueId,
        icon: item.icon,
        color: item.color,
      }));
      /* const objectToSend = {
        Id: editingFlowId,
        Name: normalizedName,
        Description: flowDescription.trim(),
        FlowType: flowMode,
        FlowJson: JSON.stringify(flowJson),
      };*/

      flowsApi
        .update({
          id: editingFlowId,
          name: normalizedName,
          description: flowDescription.trim(),
          flowType: flowMode,
          locationId: location?.id ?? '',
          flowJson: JSON.stringify(flowJson),
        })

        .then(() => {
          //Actualizo el flujo en la lista de flujos
          setFlows((previous) =>
            previous.map((flow) =>
              flow.id === editingFlowId
                ? {
                    ...flow,
                    name: normalizedName,
                    description: flowDescription.trim(),
                    flowType: flowMode,
                  }
                : flow
            )
          );
        })
        .catch(() => {});
    } else {
      flowsApi
        .create({
          name: normalizedName,
          description: flowDescription.trim(),
          flowType: flowMode,
          locationId: location?.id ?? '',
          flowJson: '[]',
        })
        .then((data) => {
          //Añadir el flujo a la lista de flujos
          setFlows((previous) => [...previous, data]);
          setActiveFlowId(data.id);
        })
        .catch(() => {});

      // setFlows((previous) => [...previous, nextFlow]);
      setActiveFlowId(null);
    }

    setFlowModalOpened(false);
  }

  function deleteFlow(flowId: string) {
    //Hago un fetch con DELETE a la api para eliminar el flujo. EL delete no devuelve nada solo un statos 200 si ha ido bien
    flowsApi
      .remove(flowId)
      .then(() => {
        setFlows((previous) => previous.filter((entry) => entry.id !== flowId));
        if (activeFlowId === flowId) {
          setActiveFlowId(null);
        }
        tree.clearSelected();
        setPendingSelectedNodeId(null);
      })
      .catch(() => {});

    /* setFlows((previous) => previous.filter((entry) => entry.id !== flowId));
    if (activeFlowId === flowId) {
      setActiveFlowId(null);
    }
    tree.clearSelected();
    setPendingSelectedNodeId(null);*/
  }

  function handleConfirmDeleteFlow() {
    if (!deletingFlow) {
      return;
    }

    deleteFlow(deletingFlow.id);
    setDeletingFlow(undefined);
  }

  function createNode(placement: 'root' | 'child' = 'root') {
    if (!activeFlow) {
      return;
    }

    if (placement === 'child' && !canCreateChild) {
      setRuleError(t('flows.selectMenuToInsertChild'));
      return;
    }

    const parentId = placement === 'child' ? (selectedNode?.id ?? null) : null;
    const nodeType: MenuNodeType = 'menu';

    const newNode: FlowMenuItem = {
      id: createId(),
      name: t('flows.newItem'),
      description: '',
      nodeType,
      parentId,
      queueSystemId: activeFlow.id,
      queueId: null,
      icon: 'house',
      color: 'grape',
      removedAt: null,
    };

    updateActiveFlow((flow) => ({ ...flow, menuItems: [...flow.menuItems, newNode] }));
    setPendingSelectedNodeId(newNode.id);
    setRuleError(null);
  }

  function deleteSelectedNode() {
    if (!selectedNode || !activeFlow) {
      return;
    }

    const descendants = collectDescendantIds(activeFlow.menuItems, selectedNode.id);
    const removeSet = new Set([selectedNode.id, ...descendants]);

    updateActiveFlow((flow) => ({
      ...flow,
      menuItems: flow.menuItems.filter((item) => !removeSet.has(item.id)),
    }));

    tree.clearSelected();
    setPendingSelectedNodeId(null);
    setRuleError(null);
  }

  function updateSelectedNode(changes: Partial<FlowMenuItem>) {
    if (!selectedNode) {
      return;
    }
    updateActiveFlow((flow) => ({
      ...flow,
      menuItems: flow.menuItems.map((item) =>
        item.id === selectedNode.id
          ? {
              ...item,
              ...changes,
            }
          : item
      ),
    }));
  }

  function handleNodeTypeChange(value: string | null) {
    if (!selectedNode) {
      return;
    }

    const nextType = (value as MenuNodeType) ?? 'menu';
    if (nextType === 'ticket' && selectedNodeChildrenCount > 0) {
      setRuleError(t('flows.cannotConvertNodeWithChildren'));
      return;
    }

    setRuleError(null);
    updateSelectedNode({
      nodeType: nextType,
      queueId: null,
      icon: nextType === 'ticket' ? 'ticket' : 'house',
    });
  }
  //Mando a la api el flujo para guardarlo
  const handleSave = () => {
    if (!activeFlow) {
      return;
    }

    const missingQueueTickets = activeFlow.menuItems.filter(
      (item) => item.nodeType === 'ticket' && !item.queueId
    );

    if (missingQueueTickets.length > 0) {
      setRuleError(
        t('flows.ticketQueueRequired', {
          names: missingQueueTickets.map((item) => item.name).join(', '),
        })
      );
      return;
    }

    setRuleError(null);

    const flujoJson = {
      flowId: activeFlow?.id,
      locationId: location?.id,
      name: activeFlow?.name,
      description: activeFlow?.description,
      mode: activeFlow?.flowType,
      menuItems: activeFlow?.menuItems.map((item) => ({
        id: item.id,
        name: item.name,
        description: item.description,
        nodeType: item.nodeType,
        parentId: item.parentId,
        queueId: item.queueId,
        icon: item.icon,
        color: item.color,
      })),
    };

    /* const objectToSend = {
      Id: flujoJson.flowId,
      Name: flujoJson.name,
      Description: flujoJson.description,
      FlowJson: JSON.stringify(flujoJson.menuItems),
    };*/

    flowsApi
      .update({
        id: flujoJson.flowId ?? '',
        name: flujoJson.name ?? '',
        description: flujoJson.description ?? '',
        flowType: flujoJson.mode ?? 0,
        locationId: flujoJson.locationId ?? '',
        flowJson: JSON.stringify(flujoJson.menuItems),
      })

      .then(() => {
        const savedFlow = flowsRef.current.find((entry) => entry.id === activeFlowId);
        setSavedMenuItemsJson(JSON.stringify(savedFlow?.menuItems ?? []));
        notifications.show({
          title: t('flows.savedTitle'),
          message: t('flows.savedMessage'),
          color: 'teal',
          autoClose: 3000,
        });
      })
      .catch(() => {});
  };

  if (!location) {
    return null;
  }

  return (
    <>
      <Modal
        opened={flowModalOpened}
        onClose={() => setFlowModalOpened(false)}
        title={t('flows.flowModalTitle')}
        centered
      >
        <Stack gap="md">
          <TextInput
            label={t('flows.flowName')}
            value={flowName}
            onChange={(event) => setFlowName(event.currentTarget.value)}
            withAsterisk
          />
          <TextInput
            label={t('flows.flowDescription')}
            value={flowDescription}
            onChange={(event) => setFlowDescription(event.currentTarget.value)}
          />
          <Select
            label={t('flows.flowMode')}
            value={flowMode}
            onChange={(value) => setFlowMode((value as number) ?? 0)}
            allowDeselect={false}
            data={[
              { value: 0, label: t('flows.TicketMachine') },
              { value: 1, label: t('flows.SetFree') },
              { value: 2, label: t('flows.ManualCall') },
            ]}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setFlowModalOpened(false)}>
              {t('common.cancel')}
            </Button>
            <Button onClick={submitFlow}>{t('flows.saveFlow')}</Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={Boolean(deletingFlow)}
        onClose={() => setDeletingFlow(undefined)}
        title={t('flows.deleteTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>{t('flows.deleteDescription', { name: deletingFlow?.name ?? '' })}</Text>

          <Group justify="flex-end">
            <Button variant="default" onClick={() => setDeletingFlow(undefined)}>
              {t('common.cancel')}
            </Button>

            <Button color="red" onClick={handleConfirmDeleteFlow}>
              {t('common.delete')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={unsavedModalOpened}
        onClose={() => setUnsavedModalOpened(false)}
        title={t('flows.unsavedTitle')}
        centered
      >
        <Stack gap="lg">
          <Text>{t('flows.unsavedMessage')}</Text>

          <Group justify="flex-end">
            <Button variant="default" onClick={() => setUnsavedModalOpened(false)}>
              {t('common.cancel')}
            </Button>

            <Button
              color="red"
              onClick={() => {
                const savedFlow = flowsRef.current.find((entry) => entry.id === activeFlowId);
                setSavedMenuItemsJson(JSON.stringify(savedFlow?.menuItems ?? []));
                setUnsavedModalOpened(false);
                setActiveFlowId(null);
              }}
            >
              {t('flows.leaveAnyway')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Stack gap="lg">
        <PageHeader
          label={t('flows.title')}
          title={location.name}
          actions={
            <Button onClick={openCreateFlowModal} leftSection={<IconPlus size={14} />}>
              {t('flows.newFlow')}
            </Button>
          }
        />

        {ruleError ? (
          <Alert color="orange" title={t('errors.requestFailed')}>
            {ruleError}
          </Alert>
        ) : null}

        {flows.length > 0 && !activeFlow ? (
          <SimpleGrid cols={{ base: 1, md: 2, xl: 3 }} spacing="md" verticalSpacing="md">
            {flows.map((flow) => (
              <Paper
                key={flow.id}
                withBorder
                radius="md"
                p="md"
                shadow={flow.id === activeFlowId ? 'sm' : 'xs'}
                className={cardHoverClasses.interactiveCard}
                style={{ cursor: 'pointer' }}
                onClick={() => {
                  setActiveFlowId(flow.id);
                  tree.clearSelected();
                }}
              >
                <Stack gap="sm">
                  <Group justify="space-between" align="flex-start" wrap="nowrap">
                    <Stack gap={2}>
                      <Group gap="xs">
                        <Text fw={700}>{flow.name}</Text>
                        {flow.id === activeFlowId ? (
                          <Badge size="xs">{t('flows.activeFlow')}</Badge>
                        ) : null}
                      </Group>
                      <Text size="sm" c="dimmed">
                        {flow.description || t('counters.noDescription')}
                      </Text>
                    </Stack>
                    <Group gap={4}>
                      <Tooltip label={t('common.edit')}>
                        <ActionIcon
                          variant="light"
                          color="blue"
                          onClick={(event) => {
                            event.stopPropagation();
                            openEditFlowModal(flow);
                          }}
                        >
                          <IconEdit size={16} />
                        </ActionIcon>
                      </Tooltip>

                      <Tooltip label={t('common.delete')}>
                        <ActionIcon
                          variant="light"
                          color="red"
                          onClick={(event) => {
                            event.stopPropagation();
                            setDeletingFlow(flow);
                          }}
                        >
                          <IconTrash size={16} />
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  </Group>

                  <Group gap="xs">
                    <Badge variant="light">{t(getFlowModeLabelKey(flow.flowType))}</Badge>
                    <Text size="xs" c="dimmed">
                      {new Date(flow.createdAt).toLocaleDateString()}
                    </Text>
                  </Group>
                </Stack>
              </Paper>
            ))}
          </SimpleGrid>
        ) : null}

        {!activeFlow ? (
          <Paper withBorder radius="md" p="xl">
            <Text>{flows.length === 0 ? t('flows.emptyFlows') : t('flows.selectFlowToEdit')}</Text>
          </Paper>
        ) : (
          <>
            <Group justify="space-between" align="center">
              <Button
                variant="light"
                leftSection={<IconArrowLeft size={14} />}
                onClick={() => {
                  if (hasUnsavedChanges) {
                    setUnsavedModalOpened(true);
                    return;
                  }

                  setActiveFlowId(null);
                }}
              >
                {t('flows.backToFlows')}
              </Button>
              {activeFlow ? (
                <Badge size="lg" variant="light">
                  {activeFlow.name}
                </Badge>
              ) : null}
            </Group>

            <Group align="flex-start" grow>
              {activeFlow.flowType === 1 ? (
                <Paper withBorder radius="md" p="xl">
                  <Text>{t('flows.noMenusForFreeFlow')}</Text>
                </Paper>
              ) : (
                <>
                  <Card withBorder radius="md" p="md" style={{ minHeight: 560 }}>
                    <Stack gap="sm">
                      <Group justify="space-between">
                        <Text fw={700}>{t('flows.treeTitle')}</Text>
                        <Group gap={6}>
                          <Tooltip label={t('flows.addItemRoot')}>
                            <ActionIcon
                              variant="light"
                              color="teal"
                              onClick={() => createNode('root')}
                            >
                              <IconHomePlus size={14} />
                            </ActionIcon>
                          </Tooltip>
                          <Tooltip
                            label={
                              canCreateChild
                                ? t('flows.addItemChild')
                                : t('flows.selectMenuToInsertChild')
                            }
                          >
                            <ActionIcon
                              variant="light"
                              color="indigo"
                              onClick={() => createNode('child')}
                              disabled={!canCreateChild}
                            >
                              <IconArrowDown size={14} />
                            </ActionIcon>
                          </Tooltip>
                          <Tooltip label={t('common.delete')}>
                            <ActionIcon
                              variant="light"
                              color="red"
                              onClick={deleteSelectedNode}
                              disabled={!selectedNode}
                            >
                              <IconTrash size={14} />
                            </ActionIcon>
                          </Tooltip>
                        </Group>
                      </Group>

                      <Text size="xs" c="dimmed">
                        {t('flows.createHint')}
                      </Text>

                      <Divider />

                      {treeData.length === 0 ? (
                        <Text c="dimmed">{t('flows.emptyTree')}</Text>
                      ) : (
                        <Tree
                          data={treeData}
                          tree={tree}
                          selectOnClick
                          expandOnClick={false}
                          expandOnSpace={false}
                          withLines
                          renderNode={({ node, elementProps }) => {
                            const item = activeItemsById.get(node.value);
                            if (!item) {
                              return <Box {...elementProps}>{String(node.label)}</Box>;
                            }

                            const itemEmoji = getNodeEmoji(item.icon);

                            return (
                              <div {...elementProps}>
                                <Group gap="xs" wrap="nowrap">
                                  <Text size="md" lh={1}>
                                    {itemEmoji}
                                  </Text>
                                  <Text size="sm">{item.name}</Text>
                                  <Badge
                                    size="xs"
                                    variant="light"
                                    color={item.nodeType === 'ticket' ? 'grape' : 'gray'}
                                  >
                                    {item.nodeType === 'menu'
                                      ? t('flows.nodeMenu')
                                      : t('flows.nodeTicket')}
                                  </Badge>
                                </Group>
                              </div>
                            );
                          }}
                        />
                      )}

                      <Divider />

                      <Group justify="flex-end">
                        <Button
                          variant="filled"
                          color="blue"
                          leftSection={<IconDeviceFloppy size={14} />}
                          onClick={handleSave}
                        >
                          {t('flows.save')}
                        </Button>
                      </Group>
                    </Stack>
                  </Card>

                  <Card withBorder radius="md" p="md" style={{ minHeight: 560 }}>
                    {!selectedNode ? (
                      <Text c="dimmed">{t('flows.selectNodeHint')}</Text>
                    ) : (
                      <Stack gap="md">
                        <Text fw={700}>{t('flows.editorTitle')}</Text>

                        <Text fw={500} size="sm">
                          {t('flows.nodeType')}
                        </Text>

                        <SegmentedControl
                          fullWidth
                          value={selectedNode.nodeType}
                          onChange={handleNodeTypeChange}
                          data={[
                            { value: 'menu', label: t('flows.nodeMenu') },
                            {
                              value: 'ticket',
                              label: t('flows.nodeTicket'),
                              disabled: selectedNodeChildrenCount > 0,
                            },
                          ]}
                        />

                        <TextInput
                          label={t('flows.nodeName')}
                          value={selectedNode.name}
                          onChange={(event) =>
                            updateSelectedNode({ name: event.currentTarget.value })
                          }
                          withAsterisk
                        />

                        <TextInput
                          label={t('flows.nodeDescription')}
                          value={selectedNode.description}
                          onChange={(event) =>
                            updateSelectedNode({ description: event.currentTarget.value })
                          }
                        />

                        <Select
                          label={t('flows.parentNode')}
                          value={selectedNode.parentId ?? ''}
                          onChange={(value) => updateSelectedNode({ parentId: value || null })}
                          data={parentOptions}
                          allowDeselect={false}
                        />

                        {selectedNode.nodeType === 'ticket' ? (
                          <>
                            <Select
                              label={t('flows.ticketType')}
                              value={selectedNode.queueId}
                              onChange={(value) => updateSelectedNode({ queueId: value })}
                              data={QueueOptions}
                              searchable
                              allowDeselect={false}
                            />

                            <Stack gap={6}>
                              <Text fw={600} size="sm">
                                {t('flows.iconLabel')}
                              </Text>
                              <Group gap="xs">
                                {iconOptions.map((option) => {
                                  const isActive = selectedNode.icon === option.key;

                                  return (
                                    <ActionIcon
                                      key={option.key}
                                      variant={isActive ? 'filled' : 'default'}
                                      color={isActive ? 'grape' : 'gray'}
                                      onClick={() => updateSelectedNode({ icon: option.key })}
                                      aria-label={t(option.labelKey)}
                                      size="lg"
                                    >
                                      <Text size="md" lh={1}>
                                        {option.emoji}
                                      </Text>
                                    </ActionIcon>
                                  );
                                })}
                              </Group>
                            </Stack>
                          </>
                        ) : null}
                      </Stack>
                    )}
                  </Card>
                </>
              )}
            </Group>
          </>
        )}
      </Stack>
    </>
  );
}
